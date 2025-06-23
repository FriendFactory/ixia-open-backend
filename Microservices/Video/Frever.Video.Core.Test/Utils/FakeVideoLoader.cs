using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Newtonsoft.Json;

namespace Frever.Video.Core.Test.Utils;

public class FakeVideoLoader : IVideoLoader
{
    public int LoadVideoPageCallCount;

    public async Task<VideoInfo[]> LoadVideoPage(
        FetchVideoInfoFrom fetchFrom,
        Func<long, int, int, Task<List<VideoWithSong>>> loader,
        Sorting sorting,
        string targetVideo,
        int takeNext,
        int takePrevious = 0
    )
    {
        LoadVideoPageCallCount++;

        long target;

        if (long.TryParse(targetVideo, out var key))
            target = key;
        else
            target = sorting == Sorting.Desc ? long.MaxValue : 0;

        var video = await loader(target, takeNext, takePrevious);

        return video.Select(
                         v => new VideoInfo
                              {
                                  Id = v.Id, Key = v.Key.ToString(), Songs = JsonConvert.DeserializeObject<SongInfo[]>(v.SongInfo)
                              }
                     )
                    .ToArray();
    }

    public async Task<VideoInfo[]> LoadVideoPage(FetchVideoInfoFrom fetchFrom, IEnumerable<VideoWithSong> video)
    {
        ArgumentNullException.ThrowIfNull(video);

        var all = video.ToList();
        return await LoadVideoPage(
                   fetchFrom,
                   (_, _, _) => Task.FromResult(all),
                   Sorting.Asc,
                   string.Empty,
                   all.Count
               );
    }

    public Task<VideoInfo[]> LoadVideoPage(FetchVideoInfoFrom fetchFrom, params Shared.MainDb.Entities.Video[] video)
    {
        return LoadVideoPage(
            fetchFrom,
            video.Select(v => new VideoWithSong {Id = v.Id, Key = v.Id, SongInfo = JsonConvert.SerializeObject(v.SongInfo)})
        );
    }
}