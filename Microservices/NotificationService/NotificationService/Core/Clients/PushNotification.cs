using Frever.Shared.MainDb.Entities;

namespace NotificationService.Core;

public class PushNotification
{
    public string Title { get; set; }
    public NotificationType Type { get; set; }
    public bool HasDataAssetId { get; set; }
}