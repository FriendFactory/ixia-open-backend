using System;
using Frever.Protobuf;

namespace NotificationService.Client.Messages;

public class NotifyNewLikeOnVideoMessage : IMessageBase
{
    public long VideoId { get; set; }

    public long CurrentGroupId { get; set; }

    [ProtoTopField(0)] public int Version { get; set; }

    [ProtoTopField(1)] public NotificationEvent Event { get; set; } = NotificationEvent.NewLikeOnVideo;

    [ProtoTopField(2)] public Guid NotificationId { get; set; }
}