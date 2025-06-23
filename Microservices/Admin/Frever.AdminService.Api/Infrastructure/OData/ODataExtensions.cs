using System;
using System.Collections.Generic;
using System.Linq;
using Common.Infrastructure.Utils;
using Microsoft.AspNet.OData.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.UriParser;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Frever.AdminService.Api.Infrastructure.OData;

public static class ODataExtensions
{
    public static IQueryable ApplyODataRequest<T>(this IQueryable<T> source, ODataQueryOptions<T> options)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        source = source.ApplyExpand(options.SelectExpand?.SelectExpandClause, string.Empty);

        return options.ApplyTo(source, AllowedQueryOptions.Select | AllowedQueryOptions.Expand);
    }

    public static IEnumerable<JObject> ApplyODataSelect(
        this IQueryable source,
        ODataQueryOptions options,
        JsonSerializerSettings settings = null
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        var sourceArr = source.Cast<object>().ToArray();

        var propsToExpand =
            (options.SelectExpand?.SelectExpandClause.SelectedItems?.OfType<ExpandedNavigationSelectItem>() ?? []).Select(
                i => i.PathToNavigationProperty.FirstSegment.Identifier.ToCamelCase()
            );

        var propsToSelect = (options.SelectExpand?.SelectExpandClause?.SelectedItems?.OfType<PathSelectItem>() ?? [])
                           .Select(i => i.SelectedPath.FirstSegment.Identifier.ToCamelCase())
                           .ToArray();

        var serializer = JsonSerializer.Create(settings);

        return sourceArr.Select(
                             v =>
                             {
                                 var obj = JObject.FromObject(v, serializer);

                                 if (propsToSelect.Length <= 0)
                                     return obj;

                                 var propNamesToRemove = obj.Properties()
                                                            .Select(p => p.Name)
                                                            .Where(
                                                                 p => !propsToSelect.Any(
                                                                          p2 => StringComparer.OrdinalIgnoreCase.Equals(p2, p)
                                                                      ) &&
                                                                      !propsToExpand.Any(
                                                                          p2 => StringComparer.OrdinalIgnoreCase.Equals(p2, p)
                                                                      )
                                                             )
                                                            .ToArray();

                                 foreach (var p in propNamesToRemove)
                                     obj.Remove(p);

                                 return obj;
                             }
                         )
                        .ToArray();
    }

    public static IQueryable<T> ApplyExpand<T>(this IQueryable<T> source, ODataQueryOptions<T> options)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(options);

        return source.ApplyExpand(options.SelectExpand?.SelectExpandClause, string.Empty);
    }

    private static IQueryable<T> ApplyExpand<T>(this IQueryable<T> source, SelectExpandClause selectExpand, string parentProperty)
        where T : class
    {
        foreach (var item in (selectExpand?.SelectedItems ?? []).OfType<ExpandedNavigationSelectItem>())
        {
            var prop = item.PathToNavigationProperty.FirstSegment.Identifier;
            var path = string.IsNullOrWhiteSpace(parentProperty) ? prop : $"{parentProperty}.{prop}";
            source = source.Include(path);

            if (item.SelectAndExpand != null)
                source = source.ApplyExpand(item.SelectAndExpand, path);
        }

        return source;
    }
}