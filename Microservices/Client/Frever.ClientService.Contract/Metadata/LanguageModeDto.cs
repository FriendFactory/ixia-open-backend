using Common.Models.Files;

namespace Frever.ClientService.Contract.Metadata;

public class LanguageModeDto : IFileMetadataOwner
{
    public long Id { get; set; }
    public string Name { get; set; }
    public FileMetadata[] Files { get; set; }
}