using Common.Models.Files;

namespace Frever.ClientService.Contract.Metadata;

public class MakeUpDto : IFileMetadataOwner
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public FileMetadata[] Files { get; set; }
}