using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Frever.Shared.MainDb;

public class WriteDbContext : MainDbContext, IWriteDb, IMigrator
{
    public WriteDbContext(ILoggerFactory loggerFactory) : base(loggerFactory) { }
    public WriteDbContext(DbContextOptions<WriteDbContext> options, ILoggerFactory loggerFactory) : base(options, loggerFactory) { }
    public WriteDbContext(DbContextOptions options, ILoggerFactory loggerFactory, bool stub) : base(options, loggerFactory, stub) { }


    public async Task Migrate()
    {
        if (await Database.CanConnectAsync())
        {
            await Database.MigrateAsync();

            await using var conn = (NpgsqlConnection) Database.GetDbConnection();
            conn.Open();
            await conn.ReloadTypesAsync();
        }
        else
        {
            throw new InvalidOperationException("Error migrating: can't connect database");
        }
    }

    public Task<IDbContextTransaction> BeginTransaction()
    {
        return Database.BeginTransactionAsync();
    }

    public NpgsqlConnection GetDbConnection()
    {
        return (NpgsqlConnection) Database.GetDbConnection();
    }

    public Task<NestedTransaction> BeginTransactionSafe()
    {
        return NestedTransactionExtension.BeginTransactionSafe(this);
    }

    public Task<NestedTransaction> BeginTransactionSafe(IsolationLevel isolationLevel)
    {
        return NestedTransactionExtension.BeginTransactionSafe(this, isolationLevel);
    }

    public Task<IDbContextTransaction> BeginTransaction(IsolationLevel isolationLevel)
    {
        return Database.BeginTransactionAsync(isolationLevel);
    }

    public Task<int> ExecuteSqlInterpolatedAsync(FormattableString sql, CancellationToken cancellationToken = default)
    {
        return Database.ExecuteSqlRawAsync(sql.Format, sql.GetArguments(), cancellationToken);
    }

    public Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
    {
        return Database.ExecuteSqlRawAsync(sql, (IEnumerable<object>) parameters);
    }

    public IQueryable<TResult> SqlQueryRaw<TResult>([NotParameterized] string sql, params object[] parameters)
    {
        return Database.SqlQueryRaw<TResult>(sql, parameters);
    }
}