using Common.Models.Files;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;

public class AiGeneratedVideoClipInput : IFileMetadataOwner
{
    public AiGeneratedVideoType Type { get; set; }
    public AiGeneratedImageInput Image { get; set; }
    public long? RefAiImageId { get; set; }
    public string Prompt { get; set; }
    public string ShortPromptSummary { get; set; }
    public string Workflow { get; set; }
    public int Seed { get; set; }
    public int LengthSec { get; set; }
    public long? UserSoundId { get; set; }
    public long Id { get; set; }
    public FileMetadata[] Files { get; set; } = [];
}