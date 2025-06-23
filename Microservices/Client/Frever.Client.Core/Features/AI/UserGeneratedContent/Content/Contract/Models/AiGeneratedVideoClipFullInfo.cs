using Common.Models.Files;
using Frever.Client.Shared.Files;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content;

public class AiGeneratedVideoClipFullInfo : IFileMetadataOwner
{
    public int Ordinal { get; set; }
    public AiGeneratedVideoType Type { get; set; }
    public AiGeneratedImageFullInfo Image { get; set; }
    public string Prompt { get; set; }
    public string ShortPromptSummary { get; set; }
    public string Tts { get; set; }
    public string Workflow { get; set; }
    public int Seed { get; set; }
    public int LengthSec { get; set; }
    public long? UserSoundId { get; set; }
    public long Id { get; set; }

    public FileMetadata[] Files { get; set; } = [];
}

public class AiGeneratedVideoClipFileConfig : DefaultFileMetadataConfiguration<Frever.Shared.MainDb.Entities.AiGeneratedVideoClip>
{
    public AiGeneratedVideoClipFileConfig()
    {
        AddMainFile("mp4");
    }
}
