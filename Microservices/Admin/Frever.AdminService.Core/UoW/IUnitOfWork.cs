using System;
using System.Threading.Tasks;
using Common.Models.Database.Interfaces;

namespace Frever.AdminService.Core.UoW;

/// <summary>
///     Tracks changes in entities and stores them at once to database.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    ///     Starts tracking of entity.
    /// </summary>
    void Track<TEntity>(TEntity entity)
        where TEntity : class;

    Task SaveChanges();

    Task<ITransaction> BeginTransaction();

    void MarkAsDeleted<TEntity>(TEntity entity)
        where TEntity : class, IEntity;

    void ClearDeletionMark<TEntity>(TEntity entity)
        where TEntity : class, IEntity;
}