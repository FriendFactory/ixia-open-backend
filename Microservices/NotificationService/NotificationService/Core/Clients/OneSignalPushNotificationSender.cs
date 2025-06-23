using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NotificationService.Core;

public interface IPushNotificationSender
{
    Task SendPush(long[] groupIds, PushNotification notification);
}

internal sealed class OneSignalPushNotificationSender : IPushNotificationSender
{
    private const string OneSignalApiUrl = "https://onesignal.com/api/v1/notifications";
    private const int TotalTitleLength = 40;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly OneSignalOptions _options;

    public OneSignalPushNotificationSender(OneSignalOptions options, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        new OneSignalOptionsValidator().ValidateAndThrow(options);

        _logger = loggerFactory.CreateLogger("OneSignalPushNotificationSender");
    }

    public async Task SendPush(long[] groupIds, PushNotification notification)
    {
        ArgumentNullException.ThrowIfNull(groupIds);
        ArgumentNullException.ThrowIfNull(notification);

        if (groupIds.Length == 0)
            return;

        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromMilliseconds(1500);
        using var request = new HttpRequestMessage(HttpMethod.Post, OneSignalApiUrl);
        request.Headers.Add("authorization", $"Basic {_options.ApiKey}");

        var message = GetNotificationText(notification.Type, notification.HasDataAssetId);

        if (notification.Title is {Length: > TotalTitleLength})
            notification.Title = $"{notification.Title[..TotalTitleLength]}...";

        var body = new CreateNotificationRequest
                   {
                       AppId = _options.AppId,
                       Heading = notification.Title == null ? null : new LocalizedData {English = notification.Title},
                       Content = new LocalizedData {English = message},
                       ExternalUserIds = groupIds.Select(g => g.ToString()).ToArray(),
                       AndroidChannelId = _options.AndroidChannelId
                   };

        request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

        try
        {
            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                _logger.LogError(
                    "Error sending push notification: status code {StatusCode}, body {Content}",
                    response.StatusCode,
                    await response.Content.ReadAsStringAsync()
                );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Got Exception while sending push notification: {Message}", e.Message);
        }
    }

    private static string GetNotificationText(NotificationType type, bool hasDataAssetId)
    {
        return type switch
               {
                   NotificationType.NewFollower                => "You have a new follower",
                   NotificationType.NewLikeOnVideo             => "You have a new like on your video",
                   NotificationType.YouTaggedOnVideo           => "You have been tagged in a video",
                   NotificationType.YourVideoRemixed           => "You have a new remix on your video",
                   NotificationType.NewFriendVideo             => "There is a new video in your following feed",
                   NotificationType.NewCommentOnVideo          => "You have a new comment on your video",
                   NotificationType.NewMentionOnVideo          => "You have been mentioned in a video",
                   NotificationType.VideoDeleted               => "Your video violated our community rules and was taken down",
                   NotificationType.NewMentionInCommentOnVideo => "Someone mentioned you in a comment",
                   NotificationType.NonCharacterTagOnVideo     => "Someone shared a video with you",
                   NotificationType.NewCommentOnVideoYouHaveCommented when !hasDataAssetId =>
                       "Someone commented on a video you commented on",
                   NotificationType.NewCommentOnVideoYouHaveCommented when true => "Someone replied to your comment",
                   _                                                            => "You have new notification"
               };
    }

    public class CreateNotificationRequest
    {
        [JsonProperty("app_id")] public string AppId { get; set; }

        [JsonProperty("contents")] public LocalizedData Content { get; set; }

        [JsonProperty("headings")] public LocalizedData Heading { get; set; }

        [JsonProperty("channel_for_external_user_ids")] public string ChannelForExternalUser { get; set; } = "push";

        [JsonProperty("include_external_user_ids")] public string[] ExternalUserIds { get; set; }

        [JsonProperty("android_channel_id")] public string AndroidChannelId { get; set; }
    }

    public class LocalizedData
    {
        [JsonProperty("en")] public string English { get; set; }
    }
}