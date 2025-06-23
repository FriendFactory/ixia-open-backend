using Common.Models.Files;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Characters.Contracts;

public class AiCharacterImageDto : IFileMetadataOwner
{
    public long Id { get; set; }
    public FileMetadata[] Files { get; set; }
}