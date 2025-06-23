using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using Common.Models.Database.Interfaces;
using Frever.AdminService.Core.Utils;
namespace Frever.AdminService.Core.Services.EntityServices;

public class DefaultReadEntityService<TEntity>(IEntityReadAlgorithm<TEntity> readAlgorithm, IUserPermissionService permissionService)
    : IReadEntityService<TEntity>
    where TEntity : class, IEntity
{
    private readonly IEntityReadAlgorithm<TEntity> _readAlgorithm = readAlgorithm ?? throw new ArgumentNullException(nameof(readAlgorithm));
    private readonly IUserPermissionService _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));

    public async Task<TEntity> GetOne(long id)
    {
        if (typeof(TEntity).IsAssignableTo(typeof(IAdminAsset)))
            await _permissionService.EnsureHasAssetReadAccess();

        if (typeof(TEntity).IsAssignableTo(typeof(IAdminCategory)))
            await _permissionService.EnsureHasCategoryReadAccess();

        return await _readAlgorithm.GetOne(id).SingleOrDefaultAsyncSafe();
    }

    public async Task<IQueryable<TEntity>> GetAll(Func<IQueryable<TEntity>, IQueryable<TEntity>> processQuery, GetAllParams parameters)
    {
        if (typeof(TEntity).IsAssignableTo(typeof(IAdminAsset)))
            await _permissionService.EnsureHasAssetReadAccess();

        if (typeof(TEntity).IsAssignableTo(typeof(IAdminCategory)))
            await _permissionService.EnsureHasCategoryReadAccess();

        ArgumentNullException.ThrowIfNull(processQuery);

        var query = _readAlgorithm.GetAll();

        query = processQuery(query);

        return query;
    }

    public void Dispose()
    {
        _readAlgorithm?.Dispose();
    }
}