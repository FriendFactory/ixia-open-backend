namespace NotificationService.Client;

/// <summary>
///     Application events that could potentially generate notifications
/// </summary>
public enum NotificationEvent
{
    NewFollower = 0,
    NewLikeOnVideo = 1,
    NewVideo = 2,
    NewCommentOnVideo = 3,
    VideoDeleted = 4,
    NewMentionInCommentOnVideo = 5,
    AiContentGenerated = 6
}