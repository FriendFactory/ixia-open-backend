namespace NotificationService.Shared.Notifications;

public class NewCommentOnVideoNotification : NotificationBase
{
    public VideoInfo CommentedVideo { get; set; }

    public GroupInfo CommentedBy { get; set; }

    public CommentInfo Comment { get; set; }
}