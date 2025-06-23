using Common.Models.Files;

namespace Frever.ClientService.Contract.Social;

public class GroupShortInfo : IFileMetadataOwner
{
    public long Id { get; set; }
    public string Nickname { get; set; }
    public FileMetadata[] Files { get; set; }
}