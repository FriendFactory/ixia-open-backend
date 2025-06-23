using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Frever.Video.Core.Utils;

public static class QueryableExtensions
{
    public static Task<TSource> SingleOrDefaultAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        switch (source.Provider)
        {
            case IAsyncQueryProvider _:
                return source.SingleOrDefaultAsync();
            default:
                return Task.FromResult(source.SingleOrDefault());
        }
    }

    public static Task<TSource> SingleOrDefaultAsyncSafe<TSource>(
        this IQueryable<TSource> source,
        Expression<Func<TSource, bool>> predicate
    )
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (predicate == null)
            throw new ArgumentNullException(nameof(predicate));

        switch (source.Provider)
        {
            case IAsyncQueryProvider _:
                return source.SingleOrDefaultAsync(predicate);
            default:
                return Task.FromResult(source.SingleOrDefault(predicate));
        }
    }

    public static Task<TSource> SingleAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        switch (source.Provider)
        {
            case IAsyncQueryProvider _:
                return source.SingleAsync();
            default:
                return Task.FromResult(source.Single());
        }
    }

    public static Task<bool> AnyAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        switch (source.Provider)
        {
            case IAsyncQueryProvider _:
                return source.AnyAsync();
            default:
                return Task.FromResult(source.Any());
        }
    }

    public static Task<int> CountAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        switch (source.Provider)
        {
            case IAsyncQueryProvider _:
                return source.CountAsync();
            default:
                return Task.FromResult(source.Count());
        }
    }

    public static Task<TSource> FirstAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        switch (source.Provider)
        {
            case IAsyncQueryProvider _:
                return source.FirstAsync();
            default:
                return Task.FromResult(source.First());
        }
    }

    public static Task<TSource> FirstOrDefaultAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        switch (source.Provider)
        {
            case IAsyncQueryProvider _:
                return source.FirstOrDefaultAsync();
            default:
                return Task.FromResult(source.FirstOrDefault());
        }
    }

    public static Task<TSource[]> ToArrayAsyncSafe<TSource>(this IQueryable<TSource> source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        switch (source.Provider)
        {
            case IAsyncQueryProvider _:
                return source.ToArrayAsync();
            default:
                return Task.FromResult(source.ToArray());
        }
    }
}