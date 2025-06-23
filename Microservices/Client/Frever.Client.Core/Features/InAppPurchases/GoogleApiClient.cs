using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Apis.AndroidPublisher.v2;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Services;

namespace Frever.Client.Core.Features.InAppPurchases;

public class GoogleApiClient(InAppPurchaseOptions options)
{
    private readonly InAppPurchaseOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public async Task<(string responseBody, HttpStatusCode statusCode, bool isSuccessfulStatusCode)> GallGet(Uri url)
    {
        using var stream = new MemoryStream(Convert.FromBase64String(_options.GoogleApiKeyBase64));

        var credentials = GoogleCredential.FromStream(stream).CreateScoped(AndroidPublisherService.Scope.Androidpublisher);


        var service = new AndroidPublisherService(
            new BaseClientService.Initializer {HttpClientInitializer = credentials, ApplicationName = "Frever"}
        );


        using var response = await service.HttpClient.GetAsync(url);
        var responseBody = await response.Content.ReadAsStringAsync();

        return (responseBody, response.StatusCode, response.IsSuccessStatusCode);
    }

    public async Task<(string responseBody, HttpStatusCode statusCode, bool isSuccessfulStatusCode)> GallGetWithToken(Uri url)
    {
        using var stream = new MemoryStream(Convert.FromBase64String(_options.GoogleApiKeyBase64));

        var credentials = GoogleCredential.FromStream(stream).CreateScoped(AndroidPublisherService.Scope.Androidpublisher);


        var service = new AndroidPublisherService(
            new BaseClientService.Initializer {HttpClientInitializer = credentials, ApplicationName = "Frever"}
        );

        var token = await GetOAuthToken(service.HttpClient, url);
        var urlWithToken = AppendToken(url, token);

        using var response = await service.HttpClient.GetAsync(urlWithToken);
        var responseBody = await response.Content.ReadAsStringAsync();

        return (responseBody, response.StatusCode, response.IsSuccessStatusCode);
    }

    private async Task<string> GetOAuthToken(ConfigurableHttpClient httpClient, Uri url)
    {
        if (httpClient.MessageHandler.Credential is ServiceAccountCredential s && s.Token == null)
        {
            using var _ = await httpClient.SendAsync(new HttpRequestMessage {Method = HttpMethod.Head, RequestUri = url});
        }

        if (httpClient.MessageHandler.Credential is ServiceAccountCredential sac)
            if (!string.IsNullOrWhiteSpace(sac.Token?.AccessToken))
                return sac.Token.AccessToken;

        throw new InvalidOperationException("Can't obtain token from Google API");
    }

    private Uri AppendToken(Uri url, string token)
    {
        var urlString = url.ToString();
        var withToken = urlString.Contains("?") ? urlString + $"&access_token={token}" : urlString + $"?access_token={token}";

        return new Uri(withToken, UriKind.Absolute);
    }
}