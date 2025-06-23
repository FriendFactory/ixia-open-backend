namespace NotificationService.Shared.Notifications;

public class YourVideoRemixedNotification : NotificationBase
{
    public VideoInfo Remix { get; set; }

    public VideoInfo RemixedFromVideo { get; set; }

    public GroupInfo RemixedBy { get; set; }
}