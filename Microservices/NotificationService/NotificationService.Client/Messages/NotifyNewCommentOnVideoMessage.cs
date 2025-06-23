using System;
using Frever.Protobuf;

namespace NotificationService.Client.Messages;

public class NotifyNewCommentOnVideoMessage : IMessageBase
{
    public long VideoId { get; set; }

    public long CommentId { get; set; }

    public long CommentedBy { get; set; }

    public long? ReplyToCommentId { get; set; }

    [ProtoTopField(0)] public int Version { get; set; }

    [ProtoTopField(1)] public NotificationEvent Event { get; set; } = NotificationEvent.NewCommentOnVideo;

    [ProtoTopField(2)] public Guid NotificationId { get; set; }
}