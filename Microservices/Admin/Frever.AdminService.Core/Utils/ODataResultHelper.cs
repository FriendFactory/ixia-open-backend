using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Query;

#pragma warning disable CS8618, CA2007, CA1819

namespace Frever.AdminService.Core.Utils;

public static class ODataResultHelper
{
    public static async Task<ResultWithCount<T>> ExecuteODataRequestWithCount<T>(
        this IQueryable<T> source,
        ODataQueryOptions<T> options,
        AllowedQueryOptions ignoreQueryOptions = AllowedQueryOptions.None
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        var result = options.ApplyTo(source, ignoreQueryOptions | AllowedQueryOptions.Select);

        var countSource = options.ApplyTo(source, AllowedQueryOptions.Skip | AllowedQueryOptions.Top | AllowedQueryOptions.OrderBy);

        return new ResultWithCount<T>
               {
                   Count = await countSource.Cast<object>().CountAsyncSafe(), Data = await result.Cast<T>().ToArrayAsyncSafe()
               };
    }
}