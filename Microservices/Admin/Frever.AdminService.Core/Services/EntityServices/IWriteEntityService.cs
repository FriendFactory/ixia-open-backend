using System;
using System.Threading.Tasks;
using Common.Models.Database.Interfaces;

namespace Frever.AdminService.Core.Services.EntityServices;

/// <summary>
///     Provides a set of operation that stores entities.
///     NOTE: Service operations could not be composed safely.
/// </summary>
public interface IWriteEntityService<TEntity> : IDisposable
    where TEntity : class, IEntity
{
    /// <summary>
    ///     Creates new record in database and returns created entity.
    /// </summary>
    /// <exception cref="FluentValidation.ValidationException">Entity data is not valid.</exception>
    /// <exception cref="EntityWriteException">Error occurred on writing entity to database.</exception>
    Task<TEntity> Create(TEntity entity, object extraData = null);

    /// <summary>
    ///     Updates record in database and returns updated record.
    /// </summary>
    /// <exception cref="FluentValidation.ValidationException">Entity data is not valid.</exception>
    /// <exception cref="EntityWriteException">Error occurred on writing entity to database.</exception>
    Task<TEntity> Update(TEntity entity, object extraData = null);

    /// <summary>
    ///     Deletes record from database (or mark it as deleted).
    /// </summary>
    /// <exception cref="FluentValidation.ValidationException">Entity data is not valid.</exception>
    /// <exception cref="EntityWriteException">Error occurred on writing entity to database.</exception>
    Task<DeleteResult> Delete(long id, object extraData = null);
}