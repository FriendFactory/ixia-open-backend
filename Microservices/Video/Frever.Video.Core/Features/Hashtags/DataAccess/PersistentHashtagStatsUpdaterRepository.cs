using System.Threading.Tasks;
using Frever.Shared.MainDb;

namespace Frever.Video.Core.Features.Hashtags.DataAccess;

internal sealed class PersistentHashtagStatsUpdaterRepository(IWriteDb db) : IHashtagStatsUpdaterRepository
{
    public async Task RefreshHashtagViewsCountAsync()
    {
        const string query = """
                             UPDATE "Hashtag"
                             SET "ViewsCount" = hashtagViewCount.viewsCountPerHashtag
                             FROM (SELECT vah."HashtagId", sum(videoViews.viewsCountPerVideo) viewsCountPerHashtag
                             FROM (SELECT "Video"."Id" AS VideoId, count("VideoId") AS viewsCountPerVideo
                             FROM "Video" INNER JOIN "Views" ON "Video"."Id" = "Views"."VideoId"
                             GROUP BY "Video"."Id"
                                 ) AS videoViews INNER JOIN "VideoAndHashtag" vah ON videoViews.VideoId = vah."VideoId"
                             GROUP BY vah."HashtagId") AS hashtagViewCount
                             WHERE "Hashtag"."Id" = hashtagViewCount."HashtagId" AND "Hashtag"."IsDeleted" = false;
                             """;

        await db.ExecuteSqlRawAsync("BEGIN TRANSACTION ISOLATION LEVEL REPEATABLE READ;" + query + "COMMIT;");
    }

    public async Task RefreshHashtagsVideoCountAsync()
    {
        const string sql = """
                           update "Hashtag"
                           set "VideoCount" = usageCountInfo.UsageCount
                           from (select "HashtagId", count("HashtagId") as UsageCount
                           from "VideoAndHashtag"
                           group by "HashtagId") as usageCountInfo
                               where "Hashtag"."Id" = usageCountInfo."HashtagId" AND "Hashtag"."IsDeleted" = false;
                           """;

        await db.ExecuteSqlRawAsync("BEGIN TRANSACTION ISOLATION LEVEL REPEATABLE READ;" + sql + "COMMIT;");
    }
}