using Common.Models.Files;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;

public class AiGeneratedImagePersonInput : IFileMetadataOwner
{
    public long ParticipantAiCharacterSelfieId { get; set; }
    public long Id { get; set; }
    public FileMetadata[] Files { get; set; } = [];
}