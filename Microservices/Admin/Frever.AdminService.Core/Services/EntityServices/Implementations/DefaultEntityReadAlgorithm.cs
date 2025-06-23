using System;
using System.Linq;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Models.Database.Interfaces;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.EntityServices;

internal class DefaultEntityReadAlgorithm<TEntity>(WriteDbContext db, UserInfo userInfo)
    : IEntityReadAlgorithm<TEntity>
    where TEntity : class, IEntity
{
    protected const int ReadinessReady = 2;
    protected const int CustomReadinessLevelIdStart = 10;

    protected WriteDbContext Db { get; } = db ?? throw new ArgumentNullException(nameof(db));
    protected UserInfo UserInfo { get; } = userInfo ?? throw new ArgumentNullException(nameof(userInfo));

    /// <summary>
    ///     Gets a query returns single entity or empty if no entity available.
    ///     Inherits all filters applied by base All() methods.
    /// </summary>
    public virtual IQueryable<TEntity> GetOne(long id)
    {
        // By default get one from cache
        return GetAccessibleEntities().Where(e => e.Id == id);
    }

    public virtual IQueryable<TEntity> GetOneNoDependencies(long id)
    {
        return GetAccessibleEntities().Where(e => e.Id == id);
    }

    /// <summary>
    ///     Gets a query for all entities.
    ///     By default inherits all filters applied by All() methods, adds AsNoTracking().
    /// </summary>
    public virtual IQueryable<TEntity> GetAll()
    {
        if (!typeof(IStageable).IsAssignableFrom(typeof(TEntity)))
            return GetAccessibleEntities().AsNoTracking();

        var set = GetAccessibleEntities().AsNoTracking();

        // QA should be able to see all assets with all levels
        if (UserInfo.AccessScopes.Contains(KnownAccessScopes.ReadinessFull))
            return set;

        if (UserInfo.AccessScopes.Contains(KnownAccessScopes.ReadinessArtists))
            return set.Cast<IStageable>().Where(e => e.ReadinessId < CustomReadinessLevelIdStart).Cast<TEntity>();

        var customCreatorsAccessLevel = UserInfo.CreatorAccessLevels.Select(l => l).Where(l => l >= CustomReadinessLevelIdStart).ToArray();

        return set.Cast<IStageable>()
                  .Where(e => e.ReadinessId == ReadinessReady || customCreatorsAccessLevel.Contains(e.ReadinessId))
                  .Cast<TEntity>();
    }

    public IQueryable<TEntity> GetForDeletion(long id)
    {
        return GetAccessibleEntities().Where(e => e.Id == id);
    }

    public void Dispose()
    {
        Db?.Dispose();
    }

    /// <summary>
    ///     Gets query for retrieving entities.
    ///     Applies basic filters or includes required for both GetAll and GetOne.
    ///     Auto-applies filtering by groups for types implementing IGroupAccessible
    /// </summary>
    protected virtual IQueryable<TEntity> GetAccessibleEntities()
    {
        return Db.Set<TEntity>();
    }
}