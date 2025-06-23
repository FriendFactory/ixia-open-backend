namespace NotificationService.Shared.Notifications;

public class NewMentionOnVideo : NotificationBase
{
    public VideoInfo MentionedVideo { get; set; }

    public GroupInfo MentionedBy { get; set; }
}