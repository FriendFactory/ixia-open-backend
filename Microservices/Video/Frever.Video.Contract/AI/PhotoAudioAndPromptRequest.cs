namespace Frever.Video.Contract.AI;

public class PhotoAudioAndPromptRequest
{
    public long GeneratedImageId { get; set; }

    public long? SongId { get; set; }
    public long? ExternalSongId { get; set; }
    public long? UserSoundId { get; set; }
    public int ActivationCueSec { get; set; }
    public int VideoDurationSec { get; set; } = 5;

    public string PromptText { get; set; }
    public long? SpeakerModeId { get; set; }
    public long? LanguageModeId { get; set; }
}