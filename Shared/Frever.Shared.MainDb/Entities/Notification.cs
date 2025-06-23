using System;
using System.Collections.Generic;

namespace Frever.Shared.MainDb.Entities;

public class Notification
{
    public Notification()
    {
        NotificationAndGroup = new HashSet<NotificationAndGroup>();
    }

    public long Id { get; set; }
    public DateTime TimeStamp { get; set; }
    public DateTime Expires { get; set; }
    public bool DeleteOnRead { get; set; }
    public NotificationType Type { get; set; }
    public long? DataGroupId { get; set; }
    public long? DataVideoId { get; set; }
    public long[] DataAssetId { get; set; }
    public long? DataRefId { get; set; }
    public long? DataRefGroupId { get; set; }
    public long? DataAiContentId { get; set; }

    public virtual ICollection<NotificationAndGroup> NotificationAndGroup { get; set; }
}

public enum NotificationType
{
    /// <summary>
    ///     Received when you get new follower.
    ///     Sent to: user who have new follower
    ///     Data Group ID: new follower group
    /// </summary>
    NewFollower,

    /// <summary>
    ///     Received when you get new like on your video
    ///     Sent to: video owner
    ///     Data Group ID: user who liked video
    ///     Data Video ID: video were liked
    /// </summary>
    NewLikeOnVideo,

    /// <summary>
    ///     Received when you're tagged on video
    ///     Sent to: user were tagged
    ///     Data Group ID: owner of video you're tagged on
    ///     Data Video ID: video you're tagged on
    /// </summary>
    YouTaggedOnVideo,

    /// <summary>
    ///     Received when your video were remixed
    ///     Sent to: owner of remix source video
    ///     Data Group ID: user who made a remix
    ///     Data Video ID: remixed video ID (you could get a original video via RemixedFrom property)
    /// </summary>
    YourVideoRemixed,

    /// <summary>
    ///     New video is posted by friend (user you mutually follows)
    ///     Send to: all friends
    ///     Data Group ID: video owner
    ///     Data Video ID: ID of new video
    /// </summary>
    NewFriendVideo,

    /// <summary>
    ///     New comment is added on your video.
    ///     Send to: owner of the video
    ///     Data group ID: user who commented
    ///     Data video ID: ID of video commented
    ///     Data ref ID: ID of the comment
    /// </summary>
    NewCommentOnVideo,

    /// <summary>
    ///     New comment is added on video you've added comment to before.
    ///     Send to: all user who commented video except video owner (there is separated notification for him)
    ///     and comment author.
    ///     DataGroupId: user who commented
    ///     DataVideoId: video been added comment to
    ///     DataRefId: ID of the comment
    ///     DataAssetId: ID of the comment replied
    /// </summary>
    NewCommentOnVideoYouHaveCommented,

    /// <summary>
    ///     Video is converted.
    ///     Send to: video owner.
    ///     DataGroupId: user who owns the video
    ///     DataVideoId: video has been converted
    /// </summary>
    YourVideoConverted,

    /// <summary>
    ///     Received when your video was deleted from CMS
    ///     Sent to: video owner
    ///     Data Video ID: video were deleted
    /// </summary>
    VideoDeleted,

    /// <summary>
    ///     Received when you're mentioned in comment to video.
    ///     DataGroupId: user who commented
    ///     DataVideoId: video been added comment to
    ///     DataRefId: ID of the comment
    ///     DataRefGroupId: ID of mentioned group
    /// </summary>
    NewMentionInCommentOnVideo,

    /// <summary>
    ///     Received when you're mentioned in video description.
    ///     DataGroupId: user who commented
    ///     DataVideoId: video been added comment to
    ///     DataRefGroupId: ID of mentioned group
    /// </summary>
    NewMentionOnVideo,

    /// <summary>
    ///     Receiving when you're (non-character tag) tagged on video
    /// </summary>
    NonCharacterTagOnVideo,

    /// <summary>
    ///  Received after AI content generation is completed
    /// </summary>
    AiContentGenerated,
}