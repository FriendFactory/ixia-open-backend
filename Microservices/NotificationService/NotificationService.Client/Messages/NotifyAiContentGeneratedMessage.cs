using System;
using Frever.Protobuf;

namespace NotificationService.Client.Messages;

public class NotifyAiContentGeneratedMessage : IMessageBase
{
    public long AiContentId { get; set; }

    public long CurrentGroupId { get; set; }

    [ProtoTopField(0)] public int Version { get; set; }

    [ProtoTopField(1)] public NotificationEvent Event { get; set; } = NotificationEvent.AiContentGenerated;

    [ProtoTopField(2)] public Guid NotificationId { get; set; }
}