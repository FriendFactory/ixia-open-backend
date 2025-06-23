using Frever.Client.Shared.AI.ComfyUi;

namespace Frever.Video.Contract.AI;

public class VideoMusicGenRequest
{
    public long GeneratedVideoId { get; set; }
    public string PromptText { get; set; }
    public MusicGenContext? Context { get; set; }
    public AudioPromptMode? AudioPromptMode { get; set; }
    public AudioAudioMode? AudioAudioMode { get; set; }
}