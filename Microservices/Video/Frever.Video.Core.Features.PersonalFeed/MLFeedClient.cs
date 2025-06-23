using System;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Infrastructure.RequestId;
using Common.Infrastructure.ServiceDiscovery;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Video.Core.Features.PersonalFeed;

public interface IMLServiceClient
{
    Task<MLServiceResponse> BuildPersonalFeed(long groupId, string experimentsHeader, decimal lon, decimal lat);
}

public class MLServiceResponse
{
    public bool Ok { get; set; }

    public MLVideoRef[] Videos { get; set; }
}

public class MLVideoRef
{
    [JsonProperty("VideoId")] public long Id { get; set; }

    [JsonProperty("GroupId")] public long GroupId { get; set; }

    [JsonProperty("SongInfo")] public SongInfo[] SongInfo { get; set; }

    [JsonProperty("Source")] public string Source { get; set; }

    public long SortOrder { get; set; }

    public override string ToString()
    {
        return $"Id={Id} GroupId={GroupId} SortOrder={SortOrder}";
    }
}

public class HttpMLVideoFeedClient : IMLServiceClient
{
    private readonly ILogger _logger;
    private readonly ServiceUrls _serviceUrls;

    public HttpMLVideoFeedClient(ServiceUrls serviceUrls, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _serviceUrls = serviceUrls ?? throw new ArgumentNullException(nameof(serviceUrls));
        _logger = loggerFactory.CreateLogger("Frever.MlFypClient");
    }

    public async Task<MLServiceResponse> BuildPersonalFeed(long groupId, string experimentsHeader, decimal lon, decimal lat)
    {
        using var scope = _logger.BeginScope(
            "[{Sip}] Build ML FYP Feed(groupId={GroupId} header={Header}): ",
            Guid.NewGuid().ToString("N"),
            groupId,
            experimentsHeader
        );

        var uriBuilder = new UriBuilder(
                             new Uri(
                                 new Uri(_serviceUrls.MachineLearning, UriKind.Absolute),
                                 new Uri("api/feed-recsys/recommend", UriKind.Relative)
                             )
                         ) {Query = $"groupId={groupId}&lon={lon}&lat={lat}"};

        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(6);
        client.DefaultRequestHeaders.Add(HttpContextHeaderAccessor.XFreverExperiments, experimentsHeader);

        using var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

        _logger.LogInformation("Start calling ML Service at GET {uri}", uriBuilder.Uri);

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Response received from ML Service: {status}, body {b}", response.StatusCode, body);

        var video = JsonConvert.DeserializeObject<MLVideoRef[]>(body);
        for (var i = 0; i < video.Length; i++)
            video[i].SortOrder = i;

        return new MLServiceResponse {Ok = true, Videos = video};
    }
}