using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Frever.AdminService.Core.Utils;

public static class QueryableExtensions
{
    public static Task<TSource> SingleOrDefaultAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Provider switch
               {
                   IAsyncQueryProvider _ => source.SingleOrDefaultAsync(),
                   _                     => Task.FromResult(source.SingleOrDefault())
               };
    }

    public static Task<TSource> SingleAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Provider switch
               {
                   IAsyncQueryProvider _ => source.SingleAsync(),
                   _                     => Task.FromResult(source.Single())
               };
    }

    public static Task<bool> AnyAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Provider switch
               {
                   IAsyncQueryProvider _ => source.AnyAsync(),
                   _                     => Task.FromResult(source.Any())
               };
    }

    public static Task<int> CountAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Provider switch
               {
                   IAsyncQueryProvider _ => source.CountAsync(),
                   _                     => Task.FromResult(source.Count())
               };
    }

    public static Task<TSource> FirstOrDefaultAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Provider switch
               {
                   IAsyncQueryProvider _ => source.FirstOrDefaultAsync(),
                   _                     => Task.FromResult(source.FirstOrDefault())
               };
    }

    public static Task<TSource[]> ToArrayAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Provider switch
               {
                   IAsyncQueryProvider _ => source.ToArrayAsync(),
                   _                     => Task.FromResult(source.ToArray())
               };
    }

    public static IOrderedQueryable<T> SortBy<T>(this IQueryable<T> source, string propertyName, bool descending)
    {
        var param = Expression.Parameter(typeof(T), "e");
        var property = Expression.PropertyOrField(param, propertyName);
        var sort = Expression.Lambda(property, param);
        var call = Expression.Call(
            typeof(Queryable),
            "OrderBy" + (descending ? "Descending" : string.Empty),
            [typeof(T), property.Type],
            source.Expression,
            Expression.Quote(sort)
        );
        return (IOrderedQueryable<T>) source.Provider.CreateQuery<T>(call);
    }

    public static async IAsyncEnumerable<TEntity> ToPaginatedEnumerableAsync<TEntity>(
        this IQueryable<TEntity> source,
        int pageSize = 100,
        [EnumeratorCancellation] CancellationToken token = default
    )
    {
        source = typeof(TEntity).IsValueType ? source.OrderBy(e => e) : source.SortBy("Id", false);

        await foreach (var pageResult in source.GetPageAsync(pageSize, token))
        {
            foreach (var entity in pageResult)
                yield return entity;
        }
    }

    private static async IAsyncEnumerable<IEnumerable<TEntity>> GetPageAsync<TEntity>(
        this IQueryable<TEntity> source,
        int pageSize = 100,
        [EnumeratorCancellation] CancellationToken token = default
    )
    {
        var page = await source.Take(pageSize).ToListAsync(token);
        long lastId;

        do
        {
            yield return page;

            var entity = page.LastOrDefault();

            if (entity == null || page.Count < pageSize)
                break;

            var entityType = entity?.GetType();
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            BinaryExpression condition;

            if (entityType.IsValueType)
            {
                if (!long.TryParse(entity.ToString(), out lastId))
                    break;

                var lastIdConst = Expression.Constant(lastId);
                condition = Expression.GreaterThan(parameter, lastIdConst);
            }
            else
            {
                var value = entity?.GetType().GetProperty("Id")?.GetValue(entity)?.ToString();

                if (!long.TryParse(value, out lastId))
                    break;

                var property = Expression.PropertyOrField(parameter, "Id");
                var lastIdConst = Expression.Constant(lastId);
                condition = Expression.GreaterThan(property, lastIdConst);
            }

            var lambda = Expression.Lambda<Func<TEntity, bool>>(condition, parameter);

            page = await source.Where(lambda).Take(pageSize).ToListAsync(token);
        }
        while (lastId > 0);
    }
}