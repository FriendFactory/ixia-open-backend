using System.Collections.Generic;
using Common.Models.Files;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;

public class AiGeneratedImageInput : IFileMetadataOwner
{
    public int Seed { get; set; }
    public string Prompt { get; set; }
    public string ShortPromptSummary { get; set; }
    public long? AiMakeupId { get; set; }
    public string Workflow { get; set; }

    public List<AiGeneratedImagePersonInput> Persons { get; set; } = [];
    public List<AiGeneratedImageSourceInput> Sources { get; set; } = [];
    public long Id { get; set; }

    public FileMetadata[] Files { get; set; } = [];
}