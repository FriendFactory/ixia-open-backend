using Common.Models.Files;

namespace Frever.ClientService.Contract.Sounds;

public class UserSoundCreateModel : IFileMetadataOwner
{
    public int Duration { get; set; }
    public string Name { get; set; }
    public long? Size { get; set; }
    public long Id { get; set; }
    public FileMetadata[] Files { get; set; }
}