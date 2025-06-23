using System.Threading.Tasks;
using Frever.Client.Core.Features.AI.Generation.Contract;
using Frever.Client.Shared.AI.ComfyUi.Contract;
using Frever.Client.Shared.AI.PixVerse;

namespace Frever.Client.Core.Features.AI.Generation;

public interface IAiGenerationService
{
    Task<ComfyUiResponse> PostTextToImageGeneration(ImageGenerationInput input, string workflowKey = null);
    Task<ComfyUiResponse> PostImageToImageGeneration(ImageGenerationInput input, string workflowKey = null);
    Task<ComfyUiResponse> PostImageStyleGeneration(ImageGenerationInput input);
    Task<ComfyUiResponse> PostImageMakeUpGeneration(long id, ImageGenerationInput input);
    Task<ComfyUiResponse> PostImageWardrobeGeneration(ImageGenerationInput input);
    Task<ComfyUiResponse> PostImageLipSyncGeneration(ImageAudioAndPromptInput input);
    Task<ComfyUiResponse> PostImageBackgroundAudioGeneration(ImageAudioAndPromptInput input);
    Task<ComfyUiResponse> PostVideoMusicGenGeneration(VideoMusicGenInput input);
    Task<ComfyUiResponse> PostVideoSfxGeneration(VideoMusicGenInput input);
    Task<ComfyUiResponse> PostVideoLipSyncGeneration(VideoAudioAndPromptInput input);
    Task<ComfyUiResponse> PostVideoOnOutputTransformation(VideoAudioAndPromptInput input);
    Task<PixVerseProgressResponse> PostVideoPixVerseGeneration(PixVerseInput input);
    Task<PixVerseProgressResponse> PostVideoPixVerseFromFileGeneration(PixVerseFileInput input, long? sourceContentId = null);
    Task<GenerationUrlDto> GetGenerationResult(string key);
}