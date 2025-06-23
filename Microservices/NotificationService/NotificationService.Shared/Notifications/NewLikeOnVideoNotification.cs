namespace NotificationService.Shared.Notifications;

public class NewLikeOnVideoNotification : NotificationBase
{
    public VideoInfo LikedVideo { get; set; }

    public GroupInfo LikedBy { get; set; }
}