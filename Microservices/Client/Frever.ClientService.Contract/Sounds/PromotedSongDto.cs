using Common.Models.Files;

namespace Frever.ClientService.Contract.Sounds;

public class PromotedSongDto : IFileMetadataOwner
{
    public long Id { get; set; }
    public long? SongId { get; set; }
    public long? ExternalSongId { get; set; }
    public FileMetadata[] Files { get; set; }
}