namespace Frever.Client.Core.Features.AI.Generation.Contract;

public class VideoAudioAndPromptInput
{
    public long AiGeneratedContentId { get; set; }

    public long? SongId { get; set; }
    public long? ExternalSongId { get; set; }
    public long? UserSoundId { get; set; }
    public int ActivationCueSec { get; set; }
    public int VideoDurationSec { get; set; }

    public string PromptText { get; set; }
    public long? SpeakerModeId { get; set; }
    public long? LanguageModeId { get; set; }
}