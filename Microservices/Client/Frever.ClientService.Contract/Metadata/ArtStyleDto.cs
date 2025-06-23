using Common.Models.Files;
using Frever.Protobuf;

namespace Frever.ClientService.Contract.Metadata;

public class ArtStyleDto : IFileMetadataOwner
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string Text { get; set; }
    public FileMetadata[] Files { get; set; }
    [ProtoNewField(1)] public long GenderId { get; set; }
}