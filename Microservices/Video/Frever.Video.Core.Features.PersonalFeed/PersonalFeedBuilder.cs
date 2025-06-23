using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Video.Contract;
using Frever.Video.Core.Features.PersonalFeed.DataAccess;
using Frever.Video.Core.Features.PersonalFeed.Tracing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Video.Core.Features.PersonalFeed;

public interface IPersonalFeedGenerator
{
    Task<VideoRef[]> GenerateFeed(long groupId, string experimentsHeader, decimal lon, decimal lat);
}

public class MLPersonalFeedGenerator(
    IPersonalFeedRepository repo,
    ILoggerFactory loggerFactory,
    IMLServiceClient fypMlClient,
    IPersonalFeedTracerFactory tracerFactory
) : IPersonalFeedGenerator
{
    private static readonly TimeSpan RepeatViewedVideoPeriodDays = TimeSpan.FromDays(7);
    private readonly ILogger log = loggerFactory.CreateLogger("Frever.Video.Feeds.PersonalFeed");

    public async Task<VideoRef[]> GenerateFeed(long groupId, string experimentsHeader, decimal lon, decimal lat)
    {
        var mlResult = await fypMlClient.BuildPersonalFeed(groupId, experimentsHeader, lon, lat);

        var result = mlResult.Videos.Select(
                                  (s, idx) =>
                                  {
                                      s.SortOrder = idx;
                                      return s;
                                  }
                              )
                             .ToArray();

        result = await Wash(groupId, result);

        log.LogInformation("AftDataashing: {Data}", JsonConvert.SerializeObject(result));

        return result.Reverse()
                     .Select(
                          (r, idx) => new VideoRef
                                      {
                                          Id = r.Id,
                                          GroupId = r.GroupId,
                                          SortOrder = idx,
                                          SongInfo = r.SongInfo ?? []
                                      }
                      )
                     .ToArray();
    }

    /// <summary>
    ///     This method can be commented out to increase performance.
    ///     It doesn't change actual response but performs and logs extra checks to ensure ML Video Server returns correct
    ///     results.
    /// </summary>
    private async Task<MLVideoRef[]> Wash(long groupId, MLVideoRef[] source)
    {
        using var scope = log.BeginScope("Washing [{Uuid}]: ", Guid.NewGuid().ToString("N"));
        await using var tracer = await tracerFactory.StartMLFeedTracing(groupId);

        await tracer.TraceOriginalMLServerResponse(source);

        var viewsOffset = DateTime.UtcNow - RepeatViewedVideoPeriodDays;
        var viewsSrc = await (await repo.GetVideoViews(groupId)).Where(v => v.Time > viewsOffset).Select(a => a.VideoId).ToArrayAsync();
        var views = new HashSet<long>(viewsSrc);
        log.LogInformation(
            "{Count} views since {Date}, views: {Ids}",
            views.Count,
            viewsOffset.ToShortDateString(),
            string.Join(", ", views.Select(id => id.ToString()))
        );
        await tracer.TraceViews(views);

        var (notViewed, viewed) = source.WhereWithExcluded(v => !views.Contains(v.Id));
        await tracer.TraceViewedVideoIds(viewed.Select(v => v.Id).ToArray());
        log.LogInformation("Not viewed: {NotViewed}, viewed: {Viewed}", notViewed.Length, viewed.Length);

        // Sergii: I do all the filters only for testing purposes and do not change the original ML result
        return notViewed;
    }
}

public static class MlFeedEnumerableExtension
{
    public static (T[] result, T[] excluded) WhereWithExcluded<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var result = new List<T>();
        var excluded = new List<T>();

        foreach (var item in source)
            if (predicate(item))
                result.Add(item);
            else
                excluded.Add(item);

        return (result.ToArray(), excluded.ToArray());
    }
}