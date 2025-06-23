namespace NotificationService.Shared.Notifications;

public class NewFriendVideoNotification : NotificationBase
{
    public VideoInfo NewVideo { get; set; }

    public GroupInfo PostedBy { get; set; }
}