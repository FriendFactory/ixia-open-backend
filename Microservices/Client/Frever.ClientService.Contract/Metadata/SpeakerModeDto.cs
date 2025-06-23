using Common.Models.Files;

namespace Frever.ClientService.Contract.Metadata;

public class SpeakerModeDto : IFileMetadataOwner
{
    public long Id { get; set; }
    public string Name { get; set; }
    public FileMetadata[] Files { get; set; }
}