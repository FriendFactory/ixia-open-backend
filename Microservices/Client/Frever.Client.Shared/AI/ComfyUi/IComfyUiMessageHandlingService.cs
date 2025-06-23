using System.Threading.Tasks;

namespace Frever.Client.Shared.AI.ComfyUi;

public interface IComfyUiMessageHandlingService
{
    Task UpdateGeneratedContentStatus(
        string partialName,
        string bucket,
        string mainSource,
        string thumbnailSource,
        string error
    );
}