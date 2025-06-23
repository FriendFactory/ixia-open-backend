using System.Threading;
using System.Threading.Tasks;
using AWS.Messaging;
using Frever.Client.Shared.AI.ComfyUi.Contract;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Shared.AI.ComfyUi;

public sealed class ComfyUiResultMessageHandler(ILogger<ComfyUiResultMessageHandler> log, IComfyUiMessageHandlingService service)
    : IMessageHandler<ComfyUiResult>
{
    public async Task<MessageProcessStatus> HandleAsync(MessageEnvelope<ComfyUiResult> messageEnvelope, CancellationToken token = default)
    {
        var message = messageEnvelope.Message;
        log.LogInformation(
            "ComfyUi result received: {PartialName}, {Bucket} {MainKey}, {CoverKey}, {ThumbnailKey}, {MaskKey}",
            message.PartialName,
            message.Bucket,
            message.MainKey,
            message.CoverKey,
            message.ThumbnailKey,
            message.MaskKey
        );
        await service.UpdateGeneratedContentStatus(
            message.PartialName,
            message.Bucket,
            message.MainKey,
            message.ThumbnailKey,
            null
        );
        return MessageProcessStatus.Success();
    }
}