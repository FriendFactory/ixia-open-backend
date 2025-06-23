namespace NotificationService.Shared.Notifications;

public class YourVideoConversionCompletedNotification : NotificationBase
{
    public VideoInfo ConvertedVideo { get; set; }

    public GroupInfo Owner { get; set; }
}