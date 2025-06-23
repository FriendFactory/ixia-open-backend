namespace NotificationService.Shared.Notifications;

public class NewMentionInCommentOnVideo : NotificationBase
{
    public VideoInfo CommentedVideo { get; set; }

    public GroupInfo CommentedBy { get; set; }

    public CommentInfo Comment { get; set; }
}