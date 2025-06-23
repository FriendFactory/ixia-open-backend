using System;
using Frever.Shared.MainDb.Entities;

namespace NotificationService.Shared.Notifications;

public class NotificationBase
{
    public long Id { get; set; }

    public DateTime Timestamp { get; set; }

    public DateTime? Expires { get; set; }

    public NotificationType NotificationType { get; set; }

    public bool HasRead { get; set; }
}