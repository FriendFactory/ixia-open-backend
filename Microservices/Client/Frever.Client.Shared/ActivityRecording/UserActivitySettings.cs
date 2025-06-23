using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Shared.ActivityRecording;

public class UserActivitySettings
{
    public OriginalVideoCreatedConfiguration OriginalVideoCreated { get; set; } = new();
    public TemplateVideoCreatedConfiguration TemplateVideoCreated { get; set; } = new();
    public TaskCompletionConfiguration TaskCompletion { get; set; } = new();
    public VideoLikeConfiguration VideoLike { get; set; } = new();
    public VideoWatchConfiguration VideoWatch { get; set; } = new();
    public LoginConfiguration Login { get; set; } = new();
}

public abstract class UserActionConfigurationBase
{
    public abstract UserActionType ActionType { get; }
}

public class OriginalVideoCreatedConfiguration : UserActionConfigurationBase
{
    public override UserActionType ActionType => UserActionType.OriginalVideoCreated;
}

public class TemplateVideoCreatedConfiguration : UserActionConfigurationBase
{
    public override UserActionType ActionType => UserActionType.TemplateVideoCreated;
}

public class TaskCompletionConfiguration : UserActionConfigurationBase
{
    public override UserActionType ActionType => UserActionType.CompleteTask;
}

public class VideoLikeConfiguration : UserActionConfigurationBase
{
    public override UserActionType ActionType => UserActionType.LikeVideo;
}

public class VideoWatchConfiguration : UserActionConfigurationBase
{
    public override UserActionType ActionType => UserActionType.WatchVideo;
}

public class LoginConfiguration : UserActionConfigurationBase
{
    public override UserActionType ActionType => UserActionType.Login;
}