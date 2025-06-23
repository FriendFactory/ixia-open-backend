using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.MusicProvider;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Frever.AdminService.Core.Services.MusicModeration.Services;

public interface IMusicIsrcService
{
    Task FillUpMissingIsrcOnExternalSongs();
}

public class SevenDigitalMusicIsrcService : IMusicIsrcService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _log;
    private readonly IWriteDb _mainDb;
    private readonly MusicProviderApiSettings _musicProviderApiSettings;
    private readonly MusicProviderOAuthSettings _musicProviderOAuthSettings;

    public SevenDigitalMusicIsrcService(
        IOptions<MusicProviderApiSettings> musicProviderApiSettings,
        IOptions<MusicProviderOAuthSettings> musicProviderOAuthSettings,
        IHttpClientFactory httpClientFactory,
        IWriteDb mainDb,
        ILoggerFactory loggerFactory
    )
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _musicProviderApiSettings = musicProviderApiSettings?.Value ?? throw new ArgumentNullException(nameof(musicProviderApiSettings));
        _musicProviderOAuthSettings =
            musicProviderOAuthSettings?.Value ?? throw new ArgumentNullException(nameof(musicProviderOAuthSettings));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));
        _log = loggerFactory.CreateLogger("Frever.Admin.7Digital");
    }

    public async Task FillUpMissingIsrcOnExternalSongs()
    {
        _log.LogInformation("Start filling up missing external song ISRC");

        var songsToFillUp = await _mainDb.ExternalSongs
                                         .Where(s => !s.IsDeleted && (s.Isrc == null || s.ArtistName == null || s.SongName == null))
                                         .ToArrayAsync();

        foreach (var song in songsToFillUp)
            try
            {
                var details = await GetExternalSongDetails(song.Id);
                if (details?.Track == null)
                {
                    _log.LogError("Error getting external song {Id} from 7Digital", song.Id);
                    continue;
                }

                song.Isrc = details.Track.Isrc;
                song.ArtistName = details.Track?.Artist?.Name;
                song.SongName = details.Track?.Title;
                _log.LogInformation(
                    "Song ID={Id}: Setting ISRC={Isrc}, Artist={ArtistName} Title={SongName}",
                    song.Id,
                    song.Isrc,
                    song.ArtistName,
                    song.SongName
                );
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error getting external song {Id} from 7Digital", song.Id);
            }

        await _mainDb.SaveChangesAsync();

        _log.LogInformation("Finish filling missing ISRC");
    }

    private async Task<ExternalSongDetails> GetExternalSongDetails(long externalTrackId)
    {
        var url =
            $"{_musicProviderApiSettings.TrackDetailsUrl}?trackId={externalTrackId}&country={_musicProviderApiSettings.CountryCode}&oauth_consumer_key={_musicProviderOAuthSettings.OAuthConsumerKey}&usageTypes={_musicProviderApiSettings.UsageTypes}";

        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            _log.LogError(
                "{ExternalSongDetailsName}: Error requesting external service. StatusCode: {ResponseStatusCode}",
                nameof(GetExternalSongDetails),
                response.StatusCode
            );
            throw AppErrorWithStatusCodeException.BadRequest("Error requesting external service", "MusicProvider");
        }

        var data = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(data))
        {
            _log.LogError($"{nameof(GetExternalSongDetails)}: Error requesting external service. Content is empty");
            throw AppErrorWithStatusCodeException.BadRequest("Error requesting external service", "MusicProvider");
        }

        var details = JsonConvert.DeserializeObject<ExternalSongDetails>(data);

        return details;
    }
}

internal class ExternalSongDetails
{
    public TrackDetails Track { get; set; }
}

internal class TrackDetails
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string Isrc { get; set; }
    public ArtistDetails Artist { get; set; }
}

internal class ArtistDetails
{
    public long Id { get; set; }
    public string Name { get; set; }
}