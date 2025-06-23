using System;
using System.Linq;
using Common.Models.Database.Interfaces;

namespace Frever.AdminService.Core.Services.EntityServices;

public interface IEntityReadAlgorithm<TEntity> : IDisposable
    where TEntity : class, IEntity
{
    /// <summary>
    ///     Gets single entity or null if entity is not found.
    ///     This method uses cached value if present.
    /// </summary>
    IQueryable<TEntity> GetOne(long id);

    /// <summary>
    ///     Get root entity without loading extra dependencies.
    ///     This method reads data from database.
    /// </summary>
    IQueryable<TEntity> GetOneNoDependencies(long id);

    /// <summary>
    ///     Gets queryable of all entities.
    ///     This method uses cache.
    /// </summary>
    IQueryable<TEntity> GetAll();

    /// <summary>
    ///     Gets single entity tracked in context.
    ///     This method doesn't use cache.
    /// </summary>
    IQueryable<TEntity> GetForDeletion(long id);
}