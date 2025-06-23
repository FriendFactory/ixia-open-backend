using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Contract;

public interface IVideoLoader
{
    /// <summary>
    ///     Loads a page of videos using loader of basic video info.
    ///     Videos can be filtered so loader can be called several times to load full page of videos.
    /// </summary>
    /// <param name="loader">
    ///     Function that loads a page of videos. Accepts targetVideo, takeNext and takePrevious as
    ///     parameters.
    /// </param>
    /// <param name="sorting"></param>
    /// <param name="targetVideo"></param>
    /// <param name="takeNext"></param>
    /// <param name="takePrevious"></param>
    /// <returns></returns>
    Task<VideoInfo[]> LoadVideoPage(
        FetchVideoInfoFrom fetchFrom,
        Func<long, int, int, Task<List<VideoWithSong>>> loader,
        Sorting sorting,
        string targetVideo,
        int takeNext,
        int takePrevious = 0
    );

    Task<VideoInfo[]> LoadVideoPage(FetchVideoInfoFrom fetchFrom, IEnumerable<VideoWithSong> video);

    Task<VideoInfo[]> LoadVideoPage(FetchVideoInfoFrom fetchFrom, params Shared.MainDb.Entities.Video[] video);
}

public enum Sorting
{
    Asc,
    Desc
}

public enum FetchVideoInfoFrom
{
    ReadDb,
    WriteDb
}