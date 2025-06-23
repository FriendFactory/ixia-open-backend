namespace Frever.Shared.MainDb.Entities;

public class NotificationAndGroup
{
    public long NotificationId { get; set; }
    public long GroupId { get; set; }
    public bool HasRead { get; set; }

    public virtual Notification Notification { get; set; }
    public virtual Group Group { get; set; }
}