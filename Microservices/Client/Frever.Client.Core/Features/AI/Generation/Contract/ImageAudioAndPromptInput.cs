using System.Collections.Generic;

namespace Frever.Client.Core.Features.AI.Generation.Contract;

public class ImageAudioAndPromptInput
{
    public long? AiGeneratedContentId { get; set; }
    public Dictionary<string, string> FileUrls { get; set; }

    public long? SongId { get; set; }
    public long? ExternalSongId { get; set; }
    public long? UserSoundId { get; set; }
    public int ActivationCueSec { get; set; }
    public int VideoDurationSec { get; set; } = 5;

    public string PromptText { get; set; }
    public long? SpeakerModeId { get; set; }
    public long? LanguageModeId { get; set; }
}