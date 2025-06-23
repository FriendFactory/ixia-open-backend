using System;
using Common.Models.Files;

namespace Frever.Shared.MainDb.Entities;

public class AiCharacterImage : IFileMetadataConfigRoot
{
    public long Id { get; set; }
    public long AiCharacterId { get; set; }
    public long? DetectedGenderId { get; set; }
    public string Type { get; set; }
    public string Status { get; set; }
    public int? DetectedAge { get; set; }
    public string AiModelRequest { get; set; }
    public string AiModelResponse { get; set; }
    public DateTime? DeletedAt { get; set; }
    public FileMetadata[] Files { get; set; }

    public virtual AiCharacter Character { get; set; }
}