using Common.Models.Files;

namespace Frever.ClientService.Contract.Sounds;

public class SongInfo : IFileMetadataOwner
{
    public long Id { get; set; }
    public bool IsNew { get; set; }
    public long GenreId { get; set; }
    public string Name { get; set; }
    public int Duration { get; set; }
    public int UsageCount { get; set; }
    public bool IsFavorite { get; set; }
    public ArtistInfo Artist { get; set; }
    public AlbumInfo Album { get; set; }
    public FileMetadata[] Files { get; set; }
}