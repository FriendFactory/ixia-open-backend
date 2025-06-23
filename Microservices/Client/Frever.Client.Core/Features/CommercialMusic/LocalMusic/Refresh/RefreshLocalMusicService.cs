using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Caching;
using CsvHelper;
using Frever.Cache.Resetting;
using Frever.Client.Core.Features.CommercialMusic.BlokurClient;
using Frever.Client.Core.Features.CommercialMusic.LicenseChecking;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Frever.Videos.Shared.MusicGeoFiltering;
using Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Client.Core.Features.CommercialMusic;

public class RefreshLocalMusicService : IRefreshLocalMusicService
{
    private const long SuspisioslySmallTrackCSVSizeBytes = 300 * 1024 * 1024;

    private readonly IBlokurClient _blokurClient;
    private readonly ICache _cache;
    private readonly CacheDependencyTracker _cacheDependencyTracker;
    private readonly IContentDeletionClient _contentDeletionClient;
    private readonly CountryCodeLookup _country;
    private readonly ILogger _log;
    private readonly IWriteDb _mainDb;
    private readonly IMusicGeoFilter _musicGeoFilter;

    public RefreshLocalMusicService(
        IBlokurClient blokurClient,
        ILoggerFactory loggerFactory,
        CountryCodeLookup country,
        CacheDependencyTracker cacheDependencyTracker,
        IWriteDb mainDb,
        IConnectionMultiplexer redisConnection,
        IMusicGeoFilter musicGeoFilter,
        IContentDeletionClient contentDeletionClient,
        ICache cache
    )
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(redisConnection);

        _blokurClient = blokurClient ?? throw new ArgumentNullException(nameof(blokurClient));
        _country = country ?? throw new ArgumentNullException(nameof(country));
        _cacheDependencyTracker = cacheDependencyTracker ?? throw new ArgumentNullException(nameof(cacheDependencyTracker));
        _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));
        _musicGeoFilter = musicGeoFilter ?? throw new ArgumentNullException(nameof(musicGeoFilter));
        _contentDeletionClient = contentDeletionClient ?? throw new ArgumentNullException(nameof(contentDeletionClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _log = loggerFactory.CreateLogger("Frever.LocalMusic");
    }

    public async Task<string> DownloadTracksCsv()
    {
        var filePath = _blokurClient.MakeFullPathToTempFileName($"Blokur_{DateTime.UtcNow:yyyy-MM-dd}.csv");

        _log.LogInformation("Path to CSV file: {p}", filePath);
        var fileInfo = new FileInfo(filePath);

        if (!fileInfo.Exists || fileInfo.Length < SuspisioslySmallTrackCSVSizeBytes)
        {
            _log.LogInformation("File doesn't exists or too small, downloading...");
            await _blokurClient.DownloadTrackCsv(filePath);
        }

        return filePath;
    }

    public async Task RefreshTrackInfoFromCsv(string path)
    {
        _log.LogInformation("Refreshing track info from {f}", path);

        var allSongs = (await _mainDb.ExternalSongs.ToArrayAsync()).ToDictionary(s => s.Id);

        _log.LogInformation("Currently {n} ISRC are active", allSongs.Values.Count(s => !s.IsDeleted && s.NotClearedSince == null));

        using var sr = new StreamReader(path);
        using var csv = new CsvReader(sr, CultureInfo.InvariantCulture);

        await csv.ReadAsync();
        csv.ReadHeader();

        var total = 0;
        var active = 0;


        var songsCleared = new HashSet<long>();
        var songsUncleared = new HashSet<long>();
        var deleted = new HashSet<long>(allSongs.Keys);

        while (await csv.ReadAsync())
        {
            var trackInfo = new
                            {
                                Id = csv.GetField<long>("Song ID"),
                                Title = csv.GetField<string>("Song Title"),
                                Version = csv.GetField<string>("Version"),
                                Isrc = csv.GetField<string>("ISRC"),
                                Artist = csv.GetField<string>("Artist"),
                                IsClearedForUse = StringComparer.OrdinalIgnoreCase.Equals("Y", csv.GetField<string>("Cleared for use")),
                                ExcludedCountries = csv.GetField<string>("Excluded Countries")
                            };
            try
            {
                if (trackInfo.Id == 10344526)
                    Debugger.Break();


                if (!trackInfo.IsClearedForUse && !allSongs.ContainsKey(trackInfo.Id))
                    // Ignore non-cleared songs are not in db
                    continue;

                ExternalSong song;
                var isNew = false;
                if (!allSongs.TryGetValue(trackInfo.Id, out song))
                {
                    song = new ExternalSong {Id = trackInfo.Id, Isrc = trackInfo.Isrc, CreatedTime = DateTime.UtcNow};
                    _mainDb.ExternalSongs.Add(song);
                    isNew = true;
                }

                if (!song.IsDeleted && !trackInfo.IsClearedForUse)
                    _log.LogWarning("Track is not cleared for use {t}", trackInfo);

                var countries = await _country.ToIso3(trackInfo.ExcludedCountries.Split(",").Select(c => c.Trim()));

                var isSongCleared = song.NotClearedSince == null;

                // Sergii: need that equality check to prevent EF update row each time modification date is changed
                if (!StringComparer.InvariantCultureIgnoreCase.Equals(song.ArtistName, trackInfo.Artist) ||
                    !StringComparer.InvariantCultureIgnoreCase.Equals(song.SongName, trackInfo.Title) ||
                    song.IsDeleted == trackInfo.IsClearedForUse || !new HashSet<string>(countries).SetEquals(song.ExcludedCountries) ||
                    isSongCleared != trackInfo.IsClearedForUse)
                {
                    var formerIsDeleted = song.IsDeleted;

                    song.ArtistName = trackInfo.Artist;
                    song.SongName = trackInfo.Title;
                    song.ExcludedCountries = countries;
                    song.IsDeleted = !trackInfo.IsClearedForUse;
                    song.NotClearedSince = trackInfo.IsClearedForUse ? default(DateTime?) : DateTime.UtcNow;
                    song.ModifiedTime = DateTime.UtcNow;

                    if (formerIsDeleted != song.IsDeleted)
                    {
                        if (song.IsDeleted)
                            songsUncleared.Add(song.Id);
                        else
                            songsCleared.Add(song.Id);
                    }

                    if (!isNew)
                        _mainDb.Entry(song).State = EntityState.Modified;
                }

                if (trackInfo.IsClearedForUse)
                    active++;

                deleted.Remove(trackInfo.Id);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error updating track information {t}", trackInfo);
            }

            total++;
        }

        foreach (var deletedId in deleted)
            if (allSongs.TryGetValue(deletedId, out var song))
            {
                song.IsDeleted = true;
                song.NotClearedSince = DateTime.UtcNow;
                _log.LogWarning("Song {id} is missing in Blokur CSV and set to not cleared and deleted", deletedId);
            }

        await _mainDb.SaveChangesAsync();

        _log.LogInformation("Song CSV processed, total={t} active={a}", total, active);
        await _cacheDependencyTracker.Reset(typeof(ExternalSong));

        foreach (var id in songsUncleared.Concat(songsCleared))
        {
            _log.LogDebug("Song {id} has changed clearance status, resetting cache...", id);
            await ResetSongCaches(id);
        }

        foreach (var id in songsUncleared)
        {
            _log.LogInformation("Song {sid} was undo clearance, deleting content", id);
            await _contentDeletionClient.DeleteExternalSongById(id);
        }

        await _cache.ClearCache();
    }

    private async Task ResetSongCaches(long externalSongId)
    {
        await _musicGeoFilter.ResetSongInfo(externalSongId);
    }
}