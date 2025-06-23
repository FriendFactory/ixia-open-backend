namespace Frever.Video.Contract.AI;

public class VideoAudioAndPromptRequest
{
    public long GeneratedVideoId { get; set; }

    public long? SongId { get; set; }
    public long? ExternalSongId { get; set; }
    public long? UserSoundId { get; set; }
    public int ActivationCueSec { get; set; }
    public int VideoDurationSec { get; set; }

    public string PromptText { get; set; }
    public long? SpeakerModeId { get; set; }
    public long? LanguageModeId { get; set; }
}