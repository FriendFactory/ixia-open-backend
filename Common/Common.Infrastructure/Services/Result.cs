using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Infrastructure.Services;

public static class ResultExtensions
{
    public static async Task Collapse(this IEnumerable<Task> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
            await item;
    }

    public static async Task<TResult> Collapse<TResult>(
        this IEnumerable<Task<TResult>> source,
        Func<TResult, TResult, TResult> reduce,
        TResult defaultResult
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(reduce);

        var result = defaultResult;
        foreach (var item in source)
        {
            var itemResult = await item;
            result = reduce(result, itemResult);
        }

        return result;
    }
}