using System;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.CacheKeys;
using Frever.Client.Core.Features.CommercialMusic.BlokurClient;
using Frever.Videos.Shared.MusicGeoFiltering;
using Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Client.Core.Features.CommercialMusic.LicenseChecking;

public class MusicLicenseCheckService : IMusicLicenseCheckService
{
    private const int ProcessSongBatchCount = 10;
    private static readonly string SongQueueCacheKey = "workers::license-check".FreverUnversionedCache();
    private readonly I7DigitalClient _7digitalClient;
    private readonly IBlokurClient _blokurClient;
    private readonly IContentDeletionClient _contentDeletionClient;
    private readonly CountryCodeLookup _countryLookup;
    private readonly ILogger _log;
    private readonly IMusicGeoFilter _musicGeoFilter;
    private readonly IDatabase _redis;
    private readonly IMusicLicenseCheckRepository _repo;

    public MusicLicenseCheckService(
        IMusicLicenseCheckRepository repo,
        IConnectionMultiplexer redisConnection,
        ILoggerFactory loggerFactory,
        I7DigitalClient a7DigitalClient,
        IBlokurClient blokurClient,
        IContentDeletionClient contentDeletionClient,
        CountryCodeLookup countryLookup,
        IMusicGeoFilter musicGeoFilter
    )
    {
        ArgumentNullException.ThrowIfNull(redisConnection);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _7digitalClient = a7DigitalClient ?? throw new ArgumentNullException(nameof(a7DigitalClient));
        _blokurClient = blokurClient ?? throw new ArgumentNullException(nameof(blokurClient));
        _contentDeletionClient = contentDeletionClient ?? throw new ArgumentNullException(nameof(contentDeletionClient));
        _countryLookup = countryLookup ?? throw new ArgumentNullException(nameof(countryLookup));
        _musicGeoFilter = musicGeoFilter ?? throw new ArgumentNullException(nameof(musicGeoFilter));
        _redis = redisConnection.GetDatabase();
        _log = loggerFactory.CreateLogger("Frever.SongLicenseCheck");
    }


    public async Task LoadUncheckedSongsToQueue()
    {
        var todayStart = DateTime.UtcNow.Date;
        var uncheckedSongs = await _repo.GetSongsCheckedBefore(todayStart).ToArrayAsync();
        foreach (var song in uncheckedSongs)
            _redis.SetAdd(SongQueueCacheKey, song.Id);

        _log.LogInformation("{Length} songs loaded to license check queue", uncheckedSongs.Length);
    }

    public async Task ProcessSongQueue()
    {
        _log.LogInformation("Process license information from queue");
        var songs = _redis.SetPop(SongQueueCacheKey, ProcessSongBatchCount);

        foreach (long songId in songs)
        {
            _log.LogInformation("Processing song {Sid}", songId);
            try
            {
                await RefreshExternalSongLicensingInfo(songId);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error processing song {Sid}", songId);
            }
        }
    }

    public async Task RefreshExternalSongLicensingInfo(long externalSongId)
    {
        var externalSong = await _repo.GetById(externalSongId).SingleOrDefaultAsync();
        if (externalSong == null)
        {
            _log.LogWarning("Song is not found: {Sid}", externalSongId);
            return;
        }

        if (string.IsNullOrWhiteSpace(externalSong.Isrc))
        {
            var trackInfo = await _7digitalClient.GetExternalSongDetails(externalSongId);
            if (trackInfo == null)
            {
                _log.LogWarning("7Digital info about {Sid} is not found", externalSongId);
                return;
            }

            externalSong.Isrc = trackInfo.Isrc;
            externalSong.ArtistName = trackInfo.Artist.Name;
            externalSong.SongName = trackInfo.Title;

            _log.LogInformation("External song {Sid} miss ISRC, refreshing from 7Digital: {Isrc}", externalSongId, externalSong.Isrc);
        }

        if (string.IsNullOrWhiteSpace(externalSong.Isrc))
        {
            _log.LogWarning("Song {Sid} hasn't ISRC, skipping", externalSongId);
            return;
        }

        var cleared = false;
        var excludedCountries = Array.Empty<string>();

        try
        {
            var info = await _blokurClient.CheckRecordingStatus(
                           new BlokurStatusTestRequest
                           {
                               Recordings =
                               [
                                   new BlokurRecordingInput
                                   {
                                       Artists = [externalSong.ArtistName],
                                       Isrc = externalSong.Isrc,
                                       Title = externalSong.ArtistName,
                                       AudioProviderRecordingId = externalSong.Id.ToString()
                                   }
                               ]
                           }
                       );

            if (info.Ok)
            {
                cleared = info.Recordings[0].Cleared;
                excludedCountries = info.Recordings[0].ExcludedCountries;
                _log.LogInformation(
                    "Song {Sid} blokur status: cleared={Cleared}, excludedCountries={Excluded}",
                    externalSong.Id,
                    cleared,
                    string.Join(",", excludedCountries)
                );
            }
            else
            {
                _log.LogWarning("Error getting license info from Blokur for song {Isrc}", externalSong.Isrc);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error requesting Blokur API for song {Isrc}", externalSong.Isrc);
        }

        externalSong.NotClearedSince = cleared ? null : externalSong.NotClearedSince ?? DateTime.UtcNow;
        externalSong.ExcludedCountries = await _countryLookup.ToIso3(excludedCountries);

        var changes = await _repo.UpdateExternalSong(externalSong);

        if (changes.clearanceChanged || changes.isrcChanged || changes.excludedCountriesChanged)
        {
            _log.LogInformation("Song {Sid} license information was changed, resetting caches", externalSong.Id);
            await ResetSongCaches(externalSongId);

            if (changes.clearanceChanged && externalSong.NotClearedSince != null)
            {
                _log.LogInformation("Song {Sid} was undo clearance, deleting content", externalSong.Id);
                await _contentDeletionClient.DeleteExternalSongById(externalSong.Id);
            }
        }
    }

    private async Task ResetSongCaches(long externalSongId)
    {
        await _musicGeoFilter.ResetSongInfo(externalSongId);
    }
}