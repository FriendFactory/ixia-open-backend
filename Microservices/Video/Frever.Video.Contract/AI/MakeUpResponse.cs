using Common.Models.Files;

namespace Frever.Video.Contract.AI;

public class MakeUpResponse : IFileMetadataOwner
{
    public long Id { get; set; }
    public long CategoryId { get; set; }
    public FileMetadata[] Files { get; set; }
}