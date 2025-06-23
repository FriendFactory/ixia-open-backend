using Common.Infrastructure.ModerationProvider;

namespace Frever.ClientService.Api.Features.Moderation;

public class ModerationResponse
{
    public string ErrorMessage { get; set; }
    public bool PassedModeration { get; set; }
    public string Reason { get; set; }

    public static ModerationResponse FromModerationResult(ModerationResult result)
    {
        return new ModerationResponse
               {
                   ErrorMessage = result.ErrorMessage, PassedModeration = result.PassedModeration, Reason = result.Reason
               };
    }
}