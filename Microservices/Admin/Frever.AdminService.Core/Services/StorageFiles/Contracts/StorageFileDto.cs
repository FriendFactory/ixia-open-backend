using Common.Models.Files;

namespace Frever.AdminService.Core.Services.StorageFiles.Contracts;

public class StorageFileDto
{
    public long Id { get; set; }

    public string Key { get; set; }

    public string Version { get; set; }

    public FileExtension Extension { get; set; }
}