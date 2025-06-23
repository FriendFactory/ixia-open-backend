using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;
using Common.Infrastructure.Utils;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Video.Core.Features.VideoInfoExtraData;
using Frever.Video.Core.Features.VideoInformation.DataAccess;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Frever.Video.Core.Features.VideoInformation;

public class CachedVideoInfoLoader(
    IVideoExtraDataProvider extraDataProvider,
    IMusicGeoFilter musicGeoFilter,
    ICurrentLocationProvider location,
    IVideoInfoRepository repo,
    ICache cache
) : IVideoLoader
{
    public async Task<VideoInfo[]> LoadVideoPage(
        FetchVideoInfoFrom fetchFrom,
        Func<long, int, int, Task<List<VideoWithSong>>> loader,
        Sorting sorting,
        string targetVideo,
        int takeNext,
        int takePrevious = 0
    )
    {
        long target;

        if (long.TryParse(targetVideo, out var key))
            target = key;
        else
            target = sorting == Sorting.Desc ? long.MaxValue : 0;

        var videos = await LoadFeed(
                         loader,
                         sorting,
                         target,
                         takeNext,
                         takePrevious
                     );

        var videoList = await GetCachedVideoInfo(fetchFrom, videos);

        await extraDataProvider.SetExtraVideoInfo(videoList);

        return videoList;
    }

    public Task<VideoInfo[]> LoadVideoPage(FetchVideoInfoFrom fetchFrom, params Frever.Shared.MainDb.Entities.Video[] video)
    {
        return LoadVideoPage(
            fetchFrom,
            (_, _, _) => Task.FromResult(
                video.Select(v => new VideoWithSong {Id = v.Id, Key = v.Id, SongInfo = JsonConvert.SerializeObject(v.SongInfo)}).ToList()
            ),
            Sorting.Asc,
            string.Empty,
            video.Length
        );
    }

    public Task<VideoInfo[]> LoadVideoPage(FetchVideoInfoFrom fetchFrom, IEnumerable<VideoWithSong> video)
    {
        ArgumentNullException.ThrowIfNull(video);

        var all = video.ToList();

        return LoadVideoPage(
            fetchFrom,
            (_, _, _) => Task.FromResult(all),
            Sorting.Asc,
            string.Empty,
            all.Count
        );
    }

    private async Task<VideoWithSong[]> LoadFeed(
        Func<long, int, int, Task<List<VideoWithSong>>> loader,
        Sorting sorting,
        long targetVideo,
        int takeNext,
        int takePrevious
    )
    {
        var videos = new List<VideoWithSong>();

        // Previous are videos before current video
        // (if videos are sorted desc [5,4,3,2,1] then for target video 3 previous would be [5,4]
        // Previous videos don't include target video
        if (takePrevious > 0)
        {
            var prev = await CollectionLoader.LoadFiltered(
                           takePrevious,
                           (loaded, prev) =>
                           {
                               var t = targetVideo;

                               if (loaded.Count > 0)
                                   t = sorting == Sorting.Desc ? loaded.Max(i => i.Key) : loaded.Min(i => i.Key);

                               return loader(t, 0, prev);
                           },
                           (Func<List<VideoWithSong>, Task<VideoWithSong[]>>) FilterFunction
                       );
            videos = prev.Concat(videos).ToList(); // Prepend previous videos to already loaded
        }

        // Next are videos after current video
        // (if videos are sorted desc [5,4,3,2,1] then for target video 3 next would be [3,2,1]
        // Next videos includes target video
        if (takeNext > 0)
        {
            var next = await CollectionLoader.LoadFiltered(
                           takeNext,
                           (loaded, take) =>
                           {
                               var t = targetVideo;
                               // If this method be used for different sorted sets,
                               // get identifier in different way
                               if (loaded.Count > 0)
                                   t = sorting == Sorting.Desc ? loaded.Min(i => i.Key) - 1 : loaded.Max(i => i.Key) + 1;

                               return loader(t, take, 0);
                           },
                           (Func<List<VideoWithSong>, Task<VideoWithSong[]>>) FilterFunction
                       );
            videos.AddRange(next);
        }

        return videos.ToArray();

        async Task<VideoWithSong[]> FilterFunction(List<VideoWithSong> v)
        {
            var result = await musicGeoFilter.FilterOutUnavailableSongs(
                             (await location.Get()).CountryIso3Code,
                             v.Select(
                                 e => new
                                      {
                                          Raw = e,
                                          SongInfo = e.SongInfo == null ? [] : JsonConvert.DeserializeObject<SongInfo[]>(e.SongInfo)
                                      }
                             ),
                             e => e.SongInfo.Where(s => s.IsExternal).Select(s => s.Id).ToArray(),
                             e => e.SongInfo.Where(s => !s.IsExternal).Select(s => s.Id).ToArray()
                         );

            return result.Select(a => a.Raw).ToArray();
        }
    }

    private async Task<VideoInfo[]> GetVideoInfoByIds(FetchVideoInfoFrom fetchFrom, long[] ids)
    {
        var videos = await repo.GetVideoInfo(fetchFrom, ids).ToArrayAsync();
        var dict = videos.ToDictionary(v => v.Id);

        return ids.Select(id => dict.TryGetValue(id, out var v) ? v : null).Where(v => v != null).ToArray();
    }

    private async Task<VideoInfo[]> GetCachedVideoInfo(FetchVideoInfoFrom fetchFrom, VideoWithSong[] videos)
    {
        var keys = videos.Select(v => VideoCacheKeys.VideoInfoKey(v.Id)).ToArray();
        var cachedVideoInfo = await cache.TryGetMany<VideoInfo>(keys);

        var result = new Dictionary<long, VideoInfo>();
        foreach (var item in cachedVideoInfo.Where(a => a != null))
            result[item.Id] = item;

        var missingVideoIds = videos.Where(v => !result.ContainsKey(v.Id)).Select(v => v.Id).ToArray();
        if (missingVideoIds.Length != 0)
        {
            var missingVideos = await GetVideoInfoByIds(fetchFrom, missingVideoIds);
            foreach (var video in missingVideos)
                await cache.Put(VideoCacheKeys.VideoInfoKey(video.Id), video, TimeSpan.FromMinutes(60));

            foreach (var item in missingVideos)
                result[item.Id] = item;
        }

        var resultVideos = videos.Select(
                                      id =>
                                      {
                                          if (!result.TryGetValue(id.Id, out var video))
                                              return null;

                                          video.Key = id.Key.ToString();
                                          return video;
                                      }
                                  )
                                 .Where(a => a != null && a.Id != 0)
                                 .ToArray();
        return resultVideos;
    }
}