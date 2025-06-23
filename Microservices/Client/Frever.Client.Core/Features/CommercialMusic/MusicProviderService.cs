using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using Common.Infrastructure;
using Common.Infrastructure.MusicProvider;
using Common.Infrastructure.Utils;
using FluentValidation;
using Frever.Videos.Shared.MusicGeoFiltering;
using Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Core.Features.CommercialMusic;

public interface IMusicProviderService
{
    Task<SignedRequestData> GetSignedRequestData(SignUrlRequest request);
    Task<byte[]> DownloadExternalSongById(long trackId);
}

public class MusicProviderService(
    IOAuthSignatureProvider oAuthSignatureProvider,
    IValidator<SignUrlRequest> validator,
    ICurrentLocationProvider currentLocationProvider,
    CountryCodeLookup countryCodeLookup,
    IUserPermissionService userPermissionService,
    IHttpClientFactory httpClientFactory,
    ILogger<MusicProviderService> logger
) : IMusicProviderService
{
    public async Task<SignedRequestData> GetSignedRequestData(SignUrlRequest request)
    {
        await userPermissionService.EnsureCurrentUserActive();
        await validator.ValidateAndThrowAsync(request);

        request.QueryParameters ??= new SortedDictionary<string, string>();
        var existingCountry = request.QueryParameters.Keys.Where(k => StringComparer.OrdinalIgnoreCase.Equals("country", k.Trim()))
                                     .ToArray();
        foreach (var k in existingCountry)
            request.QueryParameters.Remove(k);

        var countryIso2 = await GetCurrentUserCountryIso2();
        request.QueryParameters["country"] = countryIso2.ToUpper();

        var httpMethod = (MusicProviderHttpMethod) Enum.Parse(typeof(MusicProviderHttpMethod), request.HttpMethod, true);

        var result = oAuthSignatureProvider.GetSignedRequestData(httpMethod, request.BaseUrl, request.QueryParameters);

        return result;
    }

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