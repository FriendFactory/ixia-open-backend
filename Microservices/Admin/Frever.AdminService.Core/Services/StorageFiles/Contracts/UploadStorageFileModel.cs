using Common.Models.Files;

namespace Frever.AdminService.Core.Services.StorageFiles.Contracts;

public class UploadStorageFileModel
{
    public long Id { get; set; }

    public string Key { get; set; }

    public string UploadId { get; set; }

    public FileExtension Extension { get; set; }
}