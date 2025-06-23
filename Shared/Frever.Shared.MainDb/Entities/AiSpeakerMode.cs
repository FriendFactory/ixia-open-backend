using Common.Models.Files;

namespace Frever.Shared.MainDb.Entities;

public class AiSpeakerMode : IFileMetadataConfigRoot
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public FileMetadata[] Files { get; set; }
}