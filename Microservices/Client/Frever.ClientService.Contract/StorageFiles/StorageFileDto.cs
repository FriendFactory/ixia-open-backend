using Frever.ClientService.Contract.Common;

namespace Frever.ClientService.Contract.StorageFiles;

public class StorageFileDto
{
    public string Key { get; set; }
    public string Version { get; set; }
    public FileExtension Extension { get; set; }
}