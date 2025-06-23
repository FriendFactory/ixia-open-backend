using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Common.Infrastructure.Database;

public static class NestedTransactionExtension
{
    /// <summary>
    ///     Allow safe usage of transactional code if calling method can be run inside other transaction.
    /// </summary>
    public static async Task<NestedTransaction> BeginTransactionSafe(this DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        return dbContext.Database.CurrentTransaction == null
                   ? new NestedTransaction(await dbContext.Database.BeginTransactionAsync())
                   : new NestedTransaction();
    }

    /// <summary>
    ///     Allow safe usage of transactional code if calling method can be run inside other transaction.
    /// </summary>
    public static async Task<NestedTransaction> BeginTransactionSafe(this DbContext dbContext, IsolationLevel isolationLevel)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        return dbContext.Database.CurrentTransaction == null
                   ? new NestedTransaction(await dbContext.Database.BeginTransactionAsync(isolationLevel))
                   : new NestedTransaction();
    }
}

public class NestedTransaction(IDbContextTransaction transaction = null) : IDisposable, IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        if (transaction != null)
            await transaction.DisposeAsync();
    }

    public void Dispose()
    {
        transaction?.Dispose();
    }

    public async Task Commit()
    {
        if (transaction != null)
            await transaction.CommitAsync();
    }
}