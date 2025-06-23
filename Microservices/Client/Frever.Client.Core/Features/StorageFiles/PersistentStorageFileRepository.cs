using System.Linq;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.StorageFiles;

public interface IStorageFileRepository
{
    IQueryable<StorageFile> GetStorageFiles();
}

internal sealed class PersistentStorageFileRepository(IWriteDb db) : IStorageFileRepository
{
    public IQueryable<StorageFile> GetStorageFiles()
    {
        return db.StorageFile;
    }
}