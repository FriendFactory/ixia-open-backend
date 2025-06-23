using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.MusicProvider;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Frever.Client.Core.Features.CommercialMusic;

public interface I7DigitalProxyService
{
    Task<JObject> LoadPlaylistById(string id);
}

public class Http7DigitalProxyService : I7DigitalProxyService
{
    private readonly MusicProviderApiSettings _7digApiSettings;
    private readonly IMusicProviderService _7digUrlSigner;
    private readonly I7DigitalClient _client;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICurrentLocationProvider _location;
    private readonly ILogger _log;
    private readonly IMusicGeoFilter _musicFilter;

    public Http7DigitalProxyService(
        I7DigitalClient client,
        ICurrentLocationProvider location,
        IMusicGeoFilter musicFilter,
        IMusicProviderService urlSigner,
        IOptions<MusicProviderApiSettings> apiSettings,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory
    )
    {
        if (loggerFactory == null)
            throw new ArgumentNullException(nameof(loggerFactory));

        _client = client ?? throw new ArgumentNullException(nameof(client));
        _location = location ?? throw new ArgumentNullException(nameof(location));
        _musicFilter = musicFilter ?? throw new ArgumentNullException(nameof(musicFilter));
        _7digUrlSigner = urlSigner ?? throw new ArgumentNullException(nameof(urlSigner));
        _7digApiSettings = (apiSettings ?? throw new ArgumentNullException(nameof(apiSettings))).Value;
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _log = loggerFactory.CreateLogger("Frever.7DigitalProxy");
    }


    public async Task<JObject> LoadPlaylistById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(id));

        var url = $"{_7digApiSettings.ApiUrl}/playlists/{id}";

        var data = await Run7DigitalHttpRequest(
                       url,
                       "get",
                       new SortedDictionary<string, string> {{"usageTypes", "download,subscriptionstreaming,adsupportedstreaming"}}
                   );

        var tracks = data["playlist"]["tracks"] as JArray;

        var info = tracks.Cast<JObject>().Select(t => new {ExternalTrackId = long.Parse(t["trackId"].ToString()), data = t}).ToArray();
        var filtered = await _musicFilter.FilterOutUnavailableSongs(
                           (await _location.Get()).CountryIso3Code,
                           info,
                           i => [i.ExternalTrackId],
                           null
                       );

        var filteredTracks = new JArray(filtered.Select(a => a.data).ToArray());
        data["playlist"]["tracks"] = filteredTracks;

        return data;
    }

    private async Task<JObject> Run7DigitalHttpRequest(string url, string httpMethod, SortedDictionary<string, string> queryParameters)
    {
        var signedUrl = await _7digUrlSigner.GetSignedRequestData(
                            new SignUrlRequest {BaseUrl = url, HttpMethod = httpMethod, QueryParameters = queryParameters}
                        );


        using var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await client.GetAsync(signedUrl.Url);

        if (!response.IsSuccessStatusCode)
        {
            _log.LogError("{}: Error requesting external service. StatusCode: {}", nameof(Run7DigitalHttpRequest), response.StatusCode);
            throw AppErrorWithStatusCodeException.BadRequest("Error requesting external service", "MusicProvider");
        }

        var data = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(data))
        {
            _log.LogError($"{nameof(Run7DigitalHttpRequest)}: Error requesting external service. Content is empty");
            throw AppErrorWithStatusCodeException.BadRequest("Error requesting external service", "MusicProvider");
        }

        return JObject.Parse(data);
    }
}