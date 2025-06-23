namespace NotificationService.Shared.Notifications;

public class NewCommentOnVideoYouHaveCommentedNotification : NotificationBase
{
    public VideoInfo CommentedVideo { get; set; }

    public GroupInfo CommentedBy { get; set; }

    public CommentInfo Comment { get; set; }
}