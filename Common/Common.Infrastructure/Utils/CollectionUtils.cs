using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Common.Infrastructure.Utils;

public static class CollectionLoader
{
    public delegate Task<List<TElement>> LoadFunction<TElement>(List<TElement> loadedContent, int take);

    private const int MAX_LOADED_PAGES = 30;

    public static async Task<TElement[]> LoadFiltered<TElement>(
        int totalCount,
        LoadFunction<TElement> loadFunction,
        Func<List<TElement>, Task<TElement[]>> filterFunction,
        ILogger log = null
    )
    {
        log?.LogInformation("Request loading of {} elements", totalCount);

        var loaded = new List<TElement>(totalCount);
        var filtered = new List<TElement>(totalCount);

        for (var i = 0; i < MAX_LOADED_PAGES; i++)
        {
            using var _ = log?.BeginScope("Tryout {}/{}: ", i, MAX_LOADED_PAGES);

            var restCount = totalCount - filtered.Count;
            if (restCount > 0)
            {
                var nextPage = await loadFunction(loaded, restCount);

                log?.LogInformation(
                    "Loaded {}/{} elements from source, collection={}",
                    nextPage.Count,
                    restCount,
                    JsonConvert.SerializeObject(nextPage)
                );

                if (nextPage.Count == 0)
                    break; // No more items to load

                loaded.AddRange(nextPage);
                log?.LogInformation("Total {} elements loaded", loaded.Count);

                var filteredPage = await filterFunction(nextPage);
                filtered.AddRange(filteredPage.Take(restCount));

                log?.LogInformation(
                    "Filter applied, {} elements passed, filtered={}",
                    filtered.Count,
                    JsonConvert.SerializeObject(filtered)
                );

                if(nextPage.Count < restCount)
                    break; //Less items received than requested, probably no more data in db
            }
            else
            {
                break;
            }
        }

        return filtered.ToArray();
    }
}