using Common.Models.Files;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;

public class AiGeneratedImageSourceInput : IFileMetadataOwner
{
    public AiGeneratedImageSourceType Type { get; set; }
    public long Id { get; set; }
    public FileMetadata[] Files { get; set; } = [];
}