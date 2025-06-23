using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Infrastructure;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;
using Common.Infrastructure.Utils;
using Common.Models;
using Frever.Client.Shared.ActivityRecording;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Sharing.DataAccess;
using Frever.Videos.Shared.CachedVideoKpis;
using Frever.Videos.Shared.MusicGeoFiltering;
using Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.Sharing;

public interface IVideoShareService
{
    Task<SharedVideoInfo> GetSharedVideo(string videoShortGuid, string country);
    Task<string> GetSharedPlayerUrl(VideoInfo video, string singleFileVideoUrl);
    Task<VideoSharingInfo> GetVideoSharingInfo(long currentGroupId, VideoInfo video, string singleFileVideoUrl);
    Task AddVideoShare(string videoShortGuid);
}

internal sealed class VideoShareService(
    ICache cache,
    VideoServerOptions config,
    IHttpContextAccessor httpContextAccessor,
    IMusicGeoFilter musicGeoFilter,
    CountryCodeLookup countryCodeLookup,
    IVideoKpiCachingService kpiCachingService,
    IVideoShareRepository repo,
    IServiceProvider serviceProvider
) : IVideoShareService
{
    public async Task<SharedVideoInfo> GetSharedVideo(string videoShortGuid, string country)
    {
        var countryIso3 = (await countryCodeLookup.ToIso3([country])).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(countryIso3))
            throw AppErrorWithStatusCodeException.BadRequest($"Invalid country code: {country}", "InvalidCountry code");

        var video = await cache.TryGet<SharedVideoInfo>(VideoCacheKeys.SharedVideo(videoShortGuid));
        if (video == null)
            return null;

        if (!await repo.IsVideoAvailable(video.VideoId))
            return null;

        if (video.Songs != null && await musicGeoFilter.AreAnySongUnavailable(
                countryIso3,
                video.Songs?.Where(s => s.IsExternal).Select(s => s.Id),
                video.Songs?.Where(s => !s.IsExternal).Select(s => s.Id)
            ))
            throw AppErrorWithStatusCodeException.BadRequest("Video is not available in your country", "NotAvailableInYourCountry");

        var kpis = await kpiCachingService.GetVideosKpis([video.VideoId]);
        video.Kpi = kpis.GetValueOrDefault(video.VideoId);

        if (video.CurrentGroup != null)
            video.FollowersCount = await repo.GetGroupFollowerCount(video.CurrentGroup.Id);

        return video;
    }

    public async Task AddVideoShare(string videoShortGuid)
    {
        var video = await cache.TryGet<SharedVideoInfo>(VideoCacheKeys.SharedVideo(videoShortGuid));
        if (video == null)
            return;

        await using var scope = serviceProvider.CreateAsyncScope();
        var userActivityService = scope.ServiceProvider.GetRequiredService<IUserActivityRecordingService>();

        await userActivityService.OnPublishedVideoShare(video.VideoId);
    }

    public async Task<string> GetSharedPlayerUrl(VideoInfo video, string singleFileVideoUrl)
    {
        var cachedVideoInfoKey = await CacheSharedVideoInfo(video, singleFileVideoUrl);

        var hostName = httpContextAccessor.HttpContext?.Request.Host.ToString();

        var hostNameFirstChar = hostName?.Replace("content-", "").FirstOrDefault();

        return $"{config.VideoPlayerPageUrl}{hostNameFirstChar}_{cachedVideoInfoKey}";
    }

    public async Task<VideoSharingInfo> GetVideoSharingInfo(long currentGroupId, VideoInfo video, string singleFileVideoUrl)
    {
        var result = new VideoSharingInfo
                     {
                         SharedPlayerUrl = await GetSharedPlayerUrl(video, singleFileVideoUrl),
                         SoftCurrency = Constants.ShareVideoSoftCurrency,
                         RewardedShareCount = Constants.RewardedShareCount,
                         CurrentShareCount = await repo.GetVideoShareCount(currentGroupId, DateTime.UtcNow.StartOfDay())
                     };

        return result;
    }

    private async Task<string> CacheSharedVideoInfo(VideoInfo video, string singleFileVideoUrl)
    {
        var videoShareInfo = new SharedVideoInfo {VideoId = video.Id, Owner = video.Owner, VideoFileUrl = singleFileVideoUrl};

        var key = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "");

        await cache.Put(VideoCacheKeys.SharedVideo(key), videoShareInfo, TimeSpan.FromDays(7));

        return key;
    }
}