using System;

namespace NotificationService.Core;

public class CommentInfo
{
    public long Id { get; set; }
    public string Key { get; set; }
    public string GroupNickname { get; set; }
    public long VideoId { get; set; }
    public long GroupId { get; set; }
    public DateTime Time { get; set; }
    public string Text { get; set; }
    public CommentGroupInfo ReplyToComment { get; set; }
}

public class CommentGroupInfo
{
    /// <summary>
    ///     Gets or sets ID of comment replied
    /// </summary>
    public long CommentId { get; set; }

    /// <summary>
    ///     Gets or sets group ID of user had been written the comment replied
    /// </summary>
    public long GroupId { get; set; }

    /// <summary>
    ///     Gets or sets group nickname of user had been written the comment replied
    /// </summary>
    public string GroupNickname { get; set; }
}