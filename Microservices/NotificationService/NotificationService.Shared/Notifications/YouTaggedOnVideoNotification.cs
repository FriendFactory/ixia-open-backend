namespace NotificationService.Shared.Notifications;

public class YouTaggedOnVideoNotification : NotificationBase
{
    public VideoInfo TaggedOnVideo { get; set; }

    public GroupInfo TaggedBy { get; set; }
}