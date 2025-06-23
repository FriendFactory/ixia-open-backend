using System;
using Frever.Protobuf;

namespace NotificationService.Client.Messages;

public class NotifyVideoDeletedMessage : IMessageBase
{
    public long CurrentGroupId { get; set; }

    public long VideoId { get; set; }

    [ProtoTopField(0)] public int Version { get; set; }

    [ProtoTopField(1)] public NotificationEvent Event { get; set; } = NotificationEvent.VideoDeleted;

    [ProtoTopField(2)] public Guid NotificationId { get; set; }
}