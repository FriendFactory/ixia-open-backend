using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.MusicProvider;
using Common.Infrastructure.Utils;
using Frever.Videos.Shared.MusicGeoFiltering;
using Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.MusicProvider;

public interface IMusicProviderService
{
    Task<byte[]> DownloadExternalSongById(long trackId);
}

public class MusicProviderService(
    CountryCodeLookup countryCodeLookup,
    ICurrentLocationProvider currentLocationProvider,
    IOAuthSignatureProvider oAuthSignatureProvider,
    IHttpClientFactory httpClientFactory,
    ILogger<MusicProviderService> logger
) : IMusicProviderService
{
    public async Task<byte[]> DownloadExternalSongById(long trackId)
    {
        var baseUrl = UriUtils.CombineUri(MusicProviderOAuthSettings.BaseUrl[1], $"clip/{trackId}");

        var countryIso2 = await GetCurrentUserCountryIso2();
        var queryParameters = new SortedDictionary<string, string> {["country"] = countryIso2.ToUpper()};

        var signedData = oAuthSignatureProvider.GetSignedRequestData(MusicProviderHttpMethod.Get, baseUrl, queryParameters);

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/mpeg"));

        var response = await client.GetAsync(signedData.Url);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("External song download failed: {StatusCode} {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            throw AppErrorWithStatusCodeException.BadRequest("External song download error", "ExternalSongDownloadFailed");
        }

        var responseBody = await response.Content.ReadAsByteArrayAsync();

        return responseBody;
    }

    private async Task<string> GetCurrentUserCountryIso2()
    {
        var location = await currentLocationProvider.Get();
        var lookup = await countryCodeLookup.GetCountryLookup();

        if (!await countryCodeLookup.IsMusicEnabled(location.CountryIso3Code))
            throw AppErrorWithStatusCodeException.BadRequest("Commercial music is not supported in the country", "MusicIsNotSupported");

        var pair = lookup.Where(kvp => StringComparer.OrdinalIgnoreCase.Equals(kvp.Value, location.CountryIso3Code))
                         .Select(kvp => kvp.Key)
                         .FirstOrDefault();

        return pair ?? ICurrentLocationProvider.UnknownLocationFakeIso3Code.CountryIso3Code;
    }
}