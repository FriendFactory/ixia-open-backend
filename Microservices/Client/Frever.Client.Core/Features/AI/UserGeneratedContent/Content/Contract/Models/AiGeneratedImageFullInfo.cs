using System.Collections.Generic;
using Common.Models.Files;
using Frever.Client.Shared.Files;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content;

public class AiGeneratedImageFullInfo : IFileMetadataOwner
{
    public long GroupId { get; set; }

    public int NumOfCharacters { get; set; }

    public int Seed { get; set; }
    public string Prompt { get; set; }
    public string ShortPromptSummary { get; set; }
    public long? AiMakeupId { get; set; }
    public long AiArtStyleId { get; set; }
    public string Workflow { get; set; }

    public List<AiGeneratedImagePersonFullInfo> Persons { get; set; } = [];
    public List<AiGeneratedImageSourceFullInfo> Sources { get; set; } = [];
    public long Id { get; set; }

    public FileMetadata[] Files { get; set; } = [];
}

public class AiGeneratedImageFileConfig : DefaultFileMetadataConfiguration<Frever.Shared.MainDb.Entities.AiGeneratedImage>
{
    public AiGeneratedImageFileConfig()
    {
        AddMainFile("jpeg");
        AddThumbnail(128, "jpeg");
    }
}