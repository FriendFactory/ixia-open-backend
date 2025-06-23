using Common.Models.Files;

namespace Frever.Shared.MainDb.Entities;

public class AiMakeUp : IFileMetadataConfigRoot
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    public FileMetadata[] Files { get; set; }
    public virtual AiMakeUpCategory Category { get; set; }
}