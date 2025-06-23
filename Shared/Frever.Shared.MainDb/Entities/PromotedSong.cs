using Common.Models.Database.Interfaces;
using Common.Models.Files;

namespace Frever.Shared.MainDb.Entities;

public class PromotedSong : IFileMetadataConfigRoot, IAdminCategory
{
    public long Id { get; set; }
    public long? SongId { get; set; }
    public long? ExternalSongId { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    public string[] AvailableForCountries { get; set; } = [];
    public FileMetadata[] Files { get; set; }
}