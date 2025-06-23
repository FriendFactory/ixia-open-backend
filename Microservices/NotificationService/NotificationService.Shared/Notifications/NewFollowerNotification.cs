namespace NotificationService.Shared.Notifications;

public class NewFollowerNotification : NotificationBase
{
    public GroupInfo FollowedBy { get; set; }
}