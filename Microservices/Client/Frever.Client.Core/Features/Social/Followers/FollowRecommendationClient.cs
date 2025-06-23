using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Common.Infrastructure.RequestId;
using Common.Infrastructure.ServiceDiscovery;
using Common.Infrastructure.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.Social.Followers;

public class MlFollowerRecommendation
{
    [JsonProperty("groupid")] public long GroupId { get; set; }

    [JsonProperty("reason")] public string Reason { get; set; }

    [JsonProperty("common_friends_list")] public long[] CommonFriendsGroupIds { get; set; }
}

public class MlFollowerRecommendationResponse
{
    [JsonProperty("response")] public List<MlFollowerRecommendation> Recommendations { get; set; }
}

public interface IFollowRecommendationClient
{
    Task<MlFollowerRecommendation[]> GetFollowRecommendations(long groupId, string experimentsHeader);
    Task<MlFollowerRecommendation[]> GetFollowBack(long groupId, string experimentsHeader);
}

public class HttpFollowRecommendationClient : IFollowRecommendationClient
{
    private readonly ILogger _log;
    private readonly ServiceUrls _urls;

    public HttpFollowRecommendationClient(ServiceUrls urls, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _urls = urls ?? throw new ArgumentNullException(nameof(urls));
        _log = loggerFactory.CreateLogger("Frever.ML.FollowRecommendationClient");
    }

    public async Task<MlFollowerRecommendation[]> GetFollowRecommendations(long groupId, string experimentsHeader)
    {
        return await GetRecommendationsFromMlServer(
                   $"api/follow-recommendation/follow/{groupId}",
                   $"GetFollowRecommendations(groupId={groupId}): ",
                   experimentsHeader
               );
    }

    public async Task<MlFollowerRecommendation[]> GetFollowBack(long groupId, string experimentsHeader)
    {
        return await GetRecommendationsFromMlServer(
                   $"api/follow-recommendation/follow-back/{groupId}",
                   $"GetFollowBack(groupId={groupId}): ",
                   experimentsHeader
               );
    }

    private async Task<MlFollowerRecommendation[]> GetRecommendationsFromMlServer(
        string relativeUrl,
        string logScope,
        string experimentsHeader
    )
    {
        if (string.IsNullOrWhiteSpace(logScope))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(logScope));
        if (string.IsNullOrWhiteSpace(relativeUrl))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(relativeUrl));

        using var _ = _log.BeginScope(logScope);

        using var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        using var client = new HttpClient(handler);
        client.DefaultRequestHeaders.Add(HttpContextHeaderAccessor.XFreverExperiments, experimentsHeader);

        var url = UriUtils.CombineUri(_urls.MachineLearning, relativeUrl);

        _log.LogInformation("Getting from url {Url} header {Header}", url, experimentsHeader);

        try
        {
            var json = await client.GetStringAsync(url);
            _log.LogInformation("Response: {Resp}", json);

            var data = JsonConvert.DeserializeObject<MlFollowerRecommendationResponse>(json);

            return data.Recommendations.ToArray();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error getting follow recommendations from ML server");
            throw;
        }
    }
}