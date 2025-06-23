using Common.Infrastructure.Sounds;
using Common.Models.Files;
using Frever.ClientService.Contract.Social;

namespace Frever.ClientService.Contract.Sounds;

public class FavoriteSoundDto : IFileMetadataOwner
{
    public long Id { get; set; }
    public FavoriteSoundType Type { get; set; }
    public string SongName { get; set; }
    public string ArtistName { get; set; }
    public int Duration { get; set; }
    public GroupShortInfo Owner { get; set; }
    public int UsageCount { get; set; }
    public string Key { get; set; }
    public FileMetadata[] Files { get; set; }
}