using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.MusicProvider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.CommercialMusic;

public class Http7DigitalClient : I7DigitalClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _log;
    private readonly MusicProviderApiSettings _musicProviderApiSettings;
    private readonly MusicProviderOAuthSettings _musicProviderOAuthSettings;

    public Http7DigitalClient(
        IOptions<MusicProviderApiSettings> musicProviderApiSettings,
        IOptions<MusicProviderOAuthSettings> musicProviderOAuthSettings,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory
    )
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _musicProviderApiSettings = musicProviderApiSettings?.Value ?? throw new ArgumentNullException(nameof(musicProviderApiSettings));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _musicProviderOAuthSettings =
            musicProviderOAuthSettings?.Value ?? throw new ArgumentNullException(nameof(musicProviderOAuthSettings));
        _log = loggerFactory.CreateLogger("Frever.7Digital");
    }

    public async Task<TrackDetails> GetExternalSongDetails(long externalSongId)
    {
        var url =
            $"{_musicProviderApiSettings.TrackDetailsUrl}?trackId={externalSongId}&country={_musicProviderApiSettings.CountryCode}&oauth_consumer_key={_musicProviderOAuthSettings.OAuthConsumerKey}&usageTypes={_musicProviderApiSettings.UsageTypes}";

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

        var details = JsonConvert.DeserializeObject<TrackDetailsResponse>(data);

        if (details?.Track == null)
        {
            _log.LogError($"{nameof(GetExternalSongDetails)}: Error requesting external service. Deserialization error");
            throw AppErrorWithStatusCodeException.BadRequest("Error requesting external service", "MusicProvider");
        }

        return details.Track;
    }
}

public class TrackDetailsResponse
{
    [JsonProperty("track")] public TrackDetails Track { get; set; }
}