using System.Threading.Tasks;
using NotificationService.Client.Messages;

// ReSharper disable once CheckNamespace
namespace NotificationService;

/// <summary>
///     Exposes a set of methods should be called for certain events.
///     Those methods generate required notifications.
///     Each methods is called on behalf of user who initiated certain action.
/// </summary>
public interface INotificationAddingService
{
    /// <summary>
    ///     Sends notification that current user follows certain group.
    /// </summary>
    Task NotifyNewFollower(NotifyNewFollowerMessage message);

    /// <summary>
    ///     Sends notification to video owner about new like.
    /// </summary>
    Task NotifyNewLikeOnVideo(NotifyNewLikeOnVideoMessage message);

    /// <summary>
    ///     Sends notification to user friends about new video published.
    /// </summary>
    Task NotifyNewVideo(NotifyNewVideoMessage message);

    /// <summary>
    ///     Sends notification to video author about new comment added on video.
    /// </summary>
    Task NotifyNewCommentOnVideo(NotifyNewCommentOnVideoMessage message);

    /// <summary>
    ///     Sends notification to user mentioned in comment to video.
    /// </summary>
    Task NotifyNewMentionsInCommentOnVideo(NotifyNewMentionInCommentOnVideoMessage message);

    /// <summary>
    ///     Sends notification to video author about deleting video.
    /// </summary>
    Task NotifyVideoDeleted(NotifyVideoDeletedMessage message);

    /// <summary>
    /// Sends notification to AI content author after generation is complete.
    /// </summary>
    Task NotifyAiContentGenerated(NotifyAiContentGeneratedMessage message);
}