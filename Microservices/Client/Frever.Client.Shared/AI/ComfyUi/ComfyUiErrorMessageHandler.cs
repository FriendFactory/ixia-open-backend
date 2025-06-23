using System.Threading;
using System.Threading.Tasks;
using AWS.Messaging;
using Frever.Client.Shared.AI.ComfyUi.Contract;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Shared.AI.ComfyUi;

public sealed class ComfyUiErrorMessageHandler(ILogger<ComfyUiErrorMessageHandler> log, IComfyUiMessageHandlingService service)
    : IMessageHandler<ComfyUiError>
{
    public async Task<MessageProcessStatus> HandleAsync(MessageEnvelope<ComfyUiError> messageEnvelope, CancellationToken token = default)
    {
        var message = messageEnvelope.Message;
        log.LogError("ComfyUi error received: {PartialName}, {Error}", message.PartialName, message.Error);
        await service.UpdateGeneratedContentStatus(
            message.PartialName,
            null,
            null,
            null,
            message.Error
        );
        return MessageProcessStatus.Success();
    }
}