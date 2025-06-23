using Common.Models.Files;

namespace Frever.Shared.MainDb.Entities;

public class AiArtStyle : IFileMetadataConfigRoot
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Text { get; set; }
    public long GenderId { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    public FileMetadata[] Files { get; set; }
}