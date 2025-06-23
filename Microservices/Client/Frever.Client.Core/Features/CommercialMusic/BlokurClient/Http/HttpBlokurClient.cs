using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.CommercialMusic.BlokurClient.Http;

public class HttpBlokurClient : IBlokurClient
{
    private static readonly Uri ApiUri = new("https://api.blokur.com/v1/");
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IValidator<BlokurStatusTestRequest> _requestValidator = new BlokurStatusTestRequestValidator();

    private readonly HttpBlokurClientSettings _settings;

    public HttpBlokurClient(HttpBlokurClientSettings settings, IHttpClientFactory httpClientFactory)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));

        _settings.Validate();
    }

    public async Task<BlokurStatusTestResponse> CheckRecordingStatus(BlokurStatusTestRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // ReSharper disable once MethodHasAsyncOverload
        _requestValidator.ValidateAndThrow(request);

        var body = JsonConvert.SerializeObject(request);

        using var httpClient = _httpClientFactory.CreateClient();
        var uri = new Uri(ApiUri, "licensing/status");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri);
        httpRequest.Content = new StringContent(body, Encoding.UTF8, "application/json");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiToken);

        using var httpResponse = await httpClient.SendAsync(httpRequest);
        if (httpResponse.StatusCode != HttpStatusCode.OK)
            return new BlokurStatusTestResponse {Ok = false, Recordings = new BlokurRecordingStatus[] { }};

        var responseContent = await httpResponse.Content.ReadAsStringAsync();

        var response = JsonConvert.DeserializeObject<BlokurStatusTestResponse>(responseContent);
        response.Ok = true;

        return response;
    }

    public string MakeFullPathToTempFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileName));

        var path = Path.Join(_settings.TempFolder, fileName);
        return path;
    }

    public async Task DownloadTrackCsv(string fullPath)
    {
        var clearedTracksCsv = await ClearedTracksCsv();
        if (!clearedTracksCsv.Ok)
            throw new InvalidOperationException("Error getting track csv url");

        using var httpClient = _httpClientFactory.CreateClient();
        var uri = new Uri(clearedTracksCsv.ClearedTracksCsvUrl);

        await using var response = await httpClient.GetStreamAsync(uri);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        await using var file = new FileStream(fullPath, FileMode.CreateNew);
        await response.CopyToAsync(file);
    }

    private async Task<BlokurClearedTrackResponse> ClearedTracksCsv()
    {
        using var httpClient = _httpClientFactory.CreateClient();
        var uri = new Uri(ApiUri, "licensing/cleared-tracks");
        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiToken);

        using var httpResponse = await httpClient.SendAsync(httpRequest);
        if (httpResponse.StatusCode != HttpStatusCode.OK)
            return new BlokurClearedTrackResponse {Ok = false};

        var responseContent = await httpResponse.Content.ReadAsStringAsync();

        var response = JsonConvert.DeserializeObject<BlokurClearedTrackResponse>(responseContent);
        response.Ok = true;

        return response;
    }
}

public class HttpBlokurClientSettings
{
    public string ApiToken { get; set; }

    public string TempFolder { get; set; }

    public void Validate()
    {
        var inlineValidator = new InlineValidator<HttpBlokurClientSettings>();
        inlineValidator.RuleFor(e => e.ApiToken).NotEmpty().MinimumLength(1);
        inlineValidator.RuleFor(e => e.TempFolder).NotEmpty().MinimumLength(1);

        inlineValidator.ValidateAndThrow(this);
    }
}