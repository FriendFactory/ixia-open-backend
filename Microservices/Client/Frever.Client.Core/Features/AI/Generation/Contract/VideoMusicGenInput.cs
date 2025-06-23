using Frever.Client.Shared.AI.ComfyUi;

namespace Frever.Client.Core.Features.AI.Generation.Contract;

public class VideoMusicGenInput
{
    public long AiGeneratedContentId { get; set; }
    public string PromptText { get; set; }
    public MusicGenContext? Context { get; set; }
    public AudioPromptMode? AudioPromptMode { get; set; }
    public AudioAudioMode? AudioAudioMode { get; set; }
}