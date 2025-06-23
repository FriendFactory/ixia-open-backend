using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Common.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.Social.MyProfileInfo;

public interface IAppsFlyerClient
{
    Task<Guid> PostUserRecordDeletion(string androidAppsFlyerId);
}

public class HttpAppsFlyerClient : IAppsFlyerClient
{
    private const string SubjectRequestType = "erasure";
    private const string SubjectIdentityType = "appsflyer_id";
    private const string SubjectIdentityFormat = "raw";
    private const string Platform = "android";
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ILogger _log;
    private readonly AppsFlyerSettings _settings;

    public HttpAppsFlyerClient(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, AppsFlyerSettings settings)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _log = loggerFactory.CreateLogger("Frever.HttpAppsFlyerClient");
    }

    public async Task<Guid> PostUserRecordDeletion(string androidAppsFlyerId)
    {
        if (string.IsNullOrWhiteSpace(androidAppsFlyerId))
            throw new ArgumentNullException(nameof(androidAppsFlyerId));

        using var _ = _log.BeginScope("PostUserRecordDeletion(androidAppsFlyerId={AppsFlyerId})", androidAppsFlyerId);

        var identity = new SubjectIdentity {Value = androidAppsFlyerId, Type = SubjectIdentityType, Format = SubjectIdentityFormat};
        var request = new EraseUserDataRequest
                      {
                          SubjectRequestId = Guid.NewGuid(),
                          SubjectRequestType = SubjectRequestType,
                          SubmittedTime = DateTime.UtcNow,
                          PropertyId = _settings.AndroidAppId,
                          Platform = Platform,
                          SubjectIdentities = new List<SubjectIdentity> {identity}
                      };

        var body = JsonConvert.SerializeObject(request);

        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(2);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, AppsFlyerSettings.Url);
        httpRequest.Content = new StringContent(body, Encoding.UTF8);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpRequest.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);

        _log.LogInformation("POST {Url} \n {Body}", AppsFlyerSettings.Url, body);

        try
        {
            using var httpResponse = await httpClient.SendAsync(httpRequest);

            var content = await httpResponse.Content.ReadAsStringAsync();
            _log.LogInformation("Response: {Response}", content);

            if (!httpResponse.IsSuccessStatusCode)
                throw AppErrorWithStatusCodeException.BadRequest(content, "AppsFlyerError");

            return request.SubjectRequestId;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error deleting user data from AppsFlyer");
            throw;
        }
    }
}

public class EraseUserDataRequest
{
    [JsonProperty("subject_request_id")] public Guid SubjectRequestId { get; set; }
    [JsonProperty("subject_request_type")] public string SubjectRequestType { get; set; }
    [JsonProperty("submitted_time")] public DateTime SubmittedTime { get; set; }
    [JsonProperty("subject_identities")] public ICollection<SubjectIdentity> SubjectIdentities { get; set; }
    [JsonProperty("property_id")] public string PropertyId { get; set; }
    [JsonProperty("platform")] public string Platform { get; set; }
}

public class SubjectIdentity
{
    [JsonProperty("identity_type")] public string Type { get; set; }
    [JsonProperty("identity_value")] public string Value { get; set; }
    [JsonProperty("identity_format")] public string Format { get; set; }
}