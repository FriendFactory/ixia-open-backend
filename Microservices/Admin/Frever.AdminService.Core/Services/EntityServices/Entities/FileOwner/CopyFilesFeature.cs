using System.Collections.Generic;
using System.Linq;
using Common.Models.Files;

namespace Frever.AdminService.Core.Services.EntityServices;

public class CopyFilesFeature<TEntity>
{
    private readonly Dictionary<TEntity, List<FileInfo>> _files = new();

    public CopyFilesFeature<TEntity> AddEntityFiles(TEntity entity, IEnumerable<FileInfo> files)
    {
        if (!_files.ContainsKey(entity))
            _files[entity] = [];

        _files[entity].AddRange(files);

        return this;
    }

    public IEnumerable<FileInfo> GetEntityFiles(TEntity entity)
    {
        return _files.TryGetValue(entity, out var files) ? files : Enumerable.Empty<FileInfo>();
    }
}