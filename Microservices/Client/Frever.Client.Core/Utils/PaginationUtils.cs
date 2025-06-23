using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Utils;

public static class PaginationUtils
{
    public static List<TItem> TakePage<TItem>(
        this IEnumerable<TItem> source,
        long? targetId,
        int takeBefore,
        int takeAfter,
        Func<TItem, long> getId
    )
    {
        // from 5 take 2 before 2 after
        // 1 2 3 4 5 6 7 8 9 10
        // 3 4 - before
        // 5 6 - after
        // 3 4 5 6 - list
        takeBefore = Math.Clamp(takeBefore, 0, int.MaxValue);
        takeAfter = Math.Clamp(takeAfter, 0, int.MaxValue);


        if (targetId == null)
            return source.Take(takeAfter).ToList();

        var before = new Queue<TItem>();
        var after = new Queue<TItem>();
        var itemFound = false;

        foreach (var item in source)
        {
            var id = getId(item);
            if (id == targetId)
                itemFound = true;

            if (itemFound)
            {
                after.Enqueue(item);
                if (after.Count >= takeAfter)
                    break;
            }
            else
            {
                before.Enqueue(item);
                if (before.Count > takeBefore)
                    before.Dequeue();
            }
        }

        return before.Concat(after).ToList();
    }

    public static async Task<T[]> GetPaginated<T, TKey>(
        IQueryable<T> query,
        Expression<Func<T, TKey>> sortProperty,
        TKey? target,
        int takeNext,
        int takePrevious,
        bool descending = true
    )
        where T : class
        where TKey : struct, IComparable<TKey>
    {
        if (takeNext == 0 && takePrevious == 0)
            return [];

        query = descending ? query.OrderByDescending(sortProperty) : query.OrderBy(sortProperty);

        if (target == null)
            return await query.Take(takeNext).ToArrayAsync();

        var propertyName = GetPropertyName(sortProperty);

        var result = new List<T>();

        if (takeNext > 0)
        {
            var nextQuery = query.Where(e => EF.Property<TKey>(e, propertyName).CompareTo(target.Value) >= 0).Take(takeNext);
            var next = await nextQuery.ToArrayAsync();
            result.AddRange(next);
        }

        if (takePrevious > 0)
        {
            var previousQuery = (descending ? query.OrderBy(sortProperty) : query.OrderByDescending(sortProperty))
                               .Where(e => EF.Property<TKey>(e, propertyName).CompareTo(target.Value) < 0)
                               .Take(takePrevious);
            var previous = await previousQuery.ToArrayAsync();
            result.AddRange(previous);
        }

        var keySelector = sortProperty.Compile();

        return (descending ? result.OrderByDescending(keySelector) : result.OrderBy(keySelector)).ToArray();

        static string GetPropertyName(Expression<Func<T, TKey>> expression)
        {
            if (expression.Body is MemberExpression member)
                return member.Member.Name;

            throw new ArgumentException("Invalid property selector expression.");
        }
    }
}