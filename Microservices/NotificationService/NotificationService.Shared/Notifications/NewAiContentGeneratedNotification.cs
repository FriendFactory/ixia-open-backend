using Frever.ClientService.Contract.Ai;

namespace NotificationService.Shared.Notifications;

public class NewAiContentGeneratedNotification : NotificationBase
{
    public AiGeneratedContentShortInfo AiContentInfo { get; set; }
}