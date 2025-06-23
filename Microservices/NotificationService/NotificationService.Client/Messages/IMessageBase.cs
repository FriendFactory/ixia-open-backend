using System;
using Frever.Protobuf;

namespace NotificationService.Client.Messages;

public interface IMessageBase
{
    int Version { get; set; }

    NotificationEvent Event { get; set; }

    Guid NotificationId { get; set; }
}

public class MessageWithVersion : IMessageBase
{
    [ProtoTopField(0)] public int Version { get; set; }

    [ProtoTopField(1)] public NotificationEvent Event { get; set; }

    [ProtoTopField(2)] public Guid NotificationId { get; set; }
}