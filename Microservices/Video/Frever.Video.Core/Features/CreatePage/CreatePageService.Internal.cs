using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Caching.CacheKeys;
using Common.Models;

namespace Frever.Video.Core.Features.CreatePage;

public sealed partial class CreatePageService
{
    private const int TrendingCount = 200;

    private async Task<List<CreatePageRow>> GetCachedRows()
    {
        var all = await cache.GetOrCache(
                      $"{nameof(CreatePageContent)}".FreverUnversionedCache(),
                      ReadCreatePageContentFromDb,
                      TimeSpan.FromHours(2)
                  );

        return all.Rows;
    }

    private async Task<CreatePageRow> GetCachedRowById(long id)
    {
        var rows = await GetCachedRows();
        return rows.Count == 0 ? null : rows.FirstOrDefault(e => e.Id == id);
    }

    private async Task<CreatePageContent> ReadCreatePageContentFromDb()
    {
        var content = new CreatePageContent();

        var rows = await repo.GetContentRows();
        if (rows.Length == 0)
            return content;

        foreach (var r in rows)
        {
            var cr = new CreatePageRow
                     {
                         Id = r.Id,
                         Title = r.Title,
                         TestGroup = r.TestGroup,
                         SortOrder = r.SortOrder,
                         Type = r.ContentType
                     };

            var type = r.ContentQuery ?? r.ContentType;
            switch (type)
            {
                case CreatePageContentType.Hashtag:
                case CreatePageContentType.Video:
                case CreatePageContentType.Song:
                case CreatePageContentType.Image:
                    cr.ContentIds = r.ContentIds;
                    break;
                case CreatePageContentQuery.PopularHashtags:
                    cr.ContentIds = await hashtagService.GetTrendingHashtagIds(TrendingCount);
                    break;
                case CreatePageContentQuery.PopularVideoRemixes:
                    cr.ContentIds = await repo.GetTrendingVideoRemixIds(TrendingCount);
                    break;
                case CreatePageContentQuery.PopularSongs:
                    cr.ContentIds = await repo.GetTrendingExternalSongIds(TrendingCount * 2);
                    break;
            }

            content.Rows.Add(cr);
        }

        return content;
    }
}