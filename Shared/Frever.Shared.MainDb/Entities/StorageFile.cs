using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class StorageFile : IAdminCategory
{
    public long Id { get; set; }
    public string Key { get; set; }
    public string Version { get; set; }
    public string Extension { get; set; }
}