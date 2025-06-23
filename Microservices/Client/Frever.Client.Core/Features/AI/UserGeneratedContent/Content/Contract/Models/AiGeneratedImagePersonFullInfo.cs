using Common.Models.Files;
using Frever.Client.Shared.Files;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content;

public class AiGeneratedImagePersonFullInfo : IFileMetadataOwner
{
    public long ParticipantGroupId { get; set; }
    public long ParticipantAiCharacterSelfieId { get; set; }
    public long? GenderId { get; set; }
    public long Id { get; set; }
    public FileMetadata[] Files { get; set; } = [];
}

public class AiGeneratedImagePersonFileConfig : DefaultFileMetadataConfiguration<Frever.Shared.MainDb.Entities.AiGeneratedImagePerson>
{
    public AiGeneratedImagePersonFileConfig()
    {
        AddMainFile("jpeg");
        AddThumbnail(128, "jpeg");
        AddFile("cover", "jpeg", false);
    }
}