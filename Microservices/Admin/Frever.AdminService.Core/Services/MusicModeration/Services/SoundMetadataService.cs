using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Infrastructure;
using Common.Models.Database.Interfaces;
using Frever.AdminService.Core.Services.MusicModeration.Contracts;
using Frever.AdminService.Core.Utils;
using Frever.Cache.Resetting;
using Frever.Shared.MainDb;
using Microsoft.AspNet.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.MusicModeration.Services;

public interface ISoundMetadataService
{
    Task<ResultWithCount<TResult>> GetMetadata<TEntity, TResult>(ODataQueryOptions<TResult> options)
        where TEntity : class, IEntity, new()
        where TResult : class, ISoundMetadata, new();

    Task SaveMetadata<TEntity, TModel>(TModel model)
        where TEntity : class, IEntity, new()
        where TModel : class, ISoundMetadata, new();
}

public class SoundMetadataService(IWriteDb db, IMapper mapper, ICacheReset cacheReset, IUserPermissionService permissionService)
    : ISoundMetadataService
{
    public async Task<ResultWithCount<TResult>> GetMetadata<TEntity, TResult>(ODataQueryOptions<TResult> options)
        where TEntity : class, IEntity, new()
        where TResult : class, ISoundMetadata, new()
    {
        await permissionService.EnsureHasCategoryReadAccess();

        return await db.Set<TEntity>().ProjectTo<TResult>(mapper.ConfigurationProvider).ExecuteODataRequestWithCount(options);
    }

    public async Task SaveMetadata<TEntity, TModel>(TModel model)
        where TEntity : class, IEntity, new()
        where TModel : class, ISoundMetadata, new()
    {
        await permissionService.EnsureHasCategoryFullAccess();

        var entity = model.Id == 0 ? await CreateEntity<TEntity>() : await db.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == model.Id);
        if (entity == null)
            throw AppErrorWithStatusCodeException.NotFound($"{typeof(TEntity).Name} not found", "ERROR_ENTITY_NOT_FOUND");

        mapper.Map(model, entity);
        await db.SaveChangesAsync();
        await cacheReset.ResetOnDependencyChange(typeof(TEntity), null);
    }

    private async Task<T> CreateEntity<T>()
        where T : class, IEntity, new()
    {
        var entity = new T();
        await db.Set<T>().AddAsync(entity);
        return entity;
    }
}