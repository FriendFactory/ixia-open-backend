using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Models.Database.Interfaces;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Frever.AdminService.Core.UoW;

internal class EntityFrameworkUnitOfWork(WriteDbContext db) : IUnitOfWork
{
    private readonly WriteDbContext _db = db ?? throw new ArgumentNullException(nameof(db));
    private readonly DbContextChangesApplier<WriteDbContext> _dbContextChangesApplier = new(db);

    private readonly List<IEntity> _markedAsDeleted = [];

    public void Track<TEntity>(TEntity entity)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        _dbContextChangesApplier.ApplyEntityToDbContext(entity);
    }

    public Task SaveChanges()
    {
        foreach (var entity in _markedAsDeleted)
            GetType().GetMethod(nameof(MarkEntityAsDeleted)).MakeGenericMethod(entity.GetType()).Invoke(this, [entity]);

        return _db.SaveChangesAsync();
    }

    public async Task<ITransaction> BeginTransaction()
    {
        var dbTransaction = await _db.Database.BeginTransactionAsync();

        return new EfTransaction(dbTransaction);
    }

    public void MarkAsDeleted<TEntity>(TEntity entity)
        where TEntity : class, IEntity
    {
        ArgumentNullException.ThrowIfNull(entity);

        var existing = _db.Set<TEntity>().Find(entity.Id);
        _markedAsDeleted.Add(existing);
    }

    public void ClearDeletionMark<TEntity>(TEntity entity)
        where TEntity : class, IEntity
    {
        ArgumentNullException.ThrowIfNull(entity);

        var target = _markedAsDeleted.FirstOrDefault(x => entity.Id == x.Id && x.GetType() == typeof(TEntity));
        if (target != null)
            _markedAsDeleted.Remove(target);
    }

    public void Dispose()
    {
        _db?.Dispose();
        _dbContextChangesApplier?.Dispose();
    }

    public void MarkEntityAsDeleted<T>(T entity)
        where T : class, IEntity
    {
        var entityToDelete = entity;
        if (_db.Entry(entity).State == EntityState.Detached)
            entityToDelete = _db.Set<T>().Find(entity.Id);

        if (entityToDelete != null)
            _db.Remove(entityToDelete);
    }

    private class EfTransaction(IDbContextTransaction dbContextTransaction) : ITransaction
    {
        private readonly IDbContextTransaction _dbContextTransaction =
            dbContextTransaction ?? throw new ArgumentNullException(nameof(dbContextTransaction));

        private bool _isCompleted;

        public async Task Commit()
        {
            try
            {
                if (_isCompleted)
                    return;
                await _dbContextTransaction.CommitAsync();
            }
            finally
            {
                _isCompleted = true;
            }
        }

        public async Task Rollback()
        {
            try
            {
                if (_isCompleted)
                    return;
                await _dbContextTransaction.RollbackAsync();
            }
            finally
            {
                _isCompleted = true;
            }
        }

        public void Dispose()
        {
            _dbContextTransaction.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return _dbContextTransaction.DisposeAsync();
        }
    }
}