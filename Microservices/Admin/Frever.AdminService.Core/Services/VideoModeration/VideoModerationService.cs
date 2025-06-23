using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AssetStoragePathProviding;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.Aws.Crypto;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;
using Common.Infrastructure.CloudFront;
using Frever.AdminService.Core.Services.AiContent;
using Frever.AdminService.Core.Services.EntityServices;
using Frever.AdminService.Core.Services.VideoModeration.Contracts;
using Frever.AdminService.Core.Services.VideoModeration.DataAccess;
using Frever.AdminService.Core.Utils;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Videos.Shared.GeoClusters;
using Microsoft.AspNet.OData.Query;
using Microsoft.EntityFrameworkCore;
using NotificationService;
using NotificationService.Client.Messages;

namespace Frever.AdminService.Core.Services.VideoModeration;

public class VideoModerationService : IVideoModerationService
{
    private static VideoNamingHelper _staticNamingHelper;
    private static CloudFrontConfiguration _staticConfig;
    private readonly IAiContentAdminService _aiAdminService;

    private readonly ICache _cache;
    private readonly CloudFrontConfiguration _cloudFrontConfiguration;
    private readonly UserInfo _currentUser;
    private readonly IGeoClusterProvider _geoClusterProvider;
    private readonly HardDeleteAccountDataHelper _hardDeleteAccountDataHelper;
    private readonly INotificationAddingService _notificationAddingService;
    private readonly IUserPermissionService _permissionService;
    private readonly IReportVideoRepository _reportVideoRepository;
    private readonly IEntityReadAlgorithm<User> _userReadAlgorithm;
    private readonly VideoNamingHelper _videoNamingHelper;
    private readonly IVideoRepository _videoRepository;

    public VideoModerationService(
        ICache cache,
        IVideoRepository videoRepository,
        IReportVideoRepository reportVideoRepository,
        INotificationAddingService notificationAddingService,
        IEntityReadAlgorithm<User> userReadAlgorithm,
        CloudFrontConfiguration cloudFrontConfiguration,
        VideoNamingHelper videoNamingHelper,
        UserInfo currentUser,
        HardDeleteAccountDataHelper hardDeleteAccountDataHelper,
        IGeoClusterProvider geoClusterProvider,
        IUserPermissionService permissionService,
        IAiContentAdminService aiAdminService
    )
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _videoRepository = videoRepository ?? throw new ArgumentNullException(nameof(videoRepository));
        _reportVideoRepository = reportVideoRepository ?? throw new ArgumentNullException(nameof(reportVideoRepository));
        _notificationAddingService = notificationAddingService ?? throw new ArgumentNullException(nameof(notificationAddingService));
        _userReadAlgorithm = userReadAlgorithm ?? throw new ArgumentNullException(nameof(userReadAlgorithm));
        _cloudFrontConfiguration = cloudFrontConfiguration ?? throw new ArgumentNullException(nameof(cloudFrontConfiguration));
        _videoNamingHelper = videoNamingHelper ?? throw new ArgumentNullException(nameof(videoNamingHelper));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _geoClusterProvider = geoClusterProvider ?? throw new ArgumentNullException(nameof(geoClusterProvider));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _aiAdminService = aiAdminService ?? throw new ArgumentNullException(nameof(aiAdminService));
        _hardDeleteAccountDataHelper = hardDeleteAccountDataHelper ?? throw new ArgumentNullException(nameof(hardDeleteAccountDataHelper));

        _staticNamingHelper ??= videoNamingHelper;
        _staticConfig ??= cloudFrontConfiguration;
    }

    public async Task<IQueryable<VideoDto>> GetAllVideos(bool? isFeatured, VideoAccess? access, string countryIso3, string languageIso3)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var result = GetVideoInfo().Where(e => access == null || e.Access == access).AsNoTracking();

        if (isFeatured.HasValue)
        {
            var groupIds = await _userReadAlgorithm.GetAll().Where(u => u.IsFeatured).Select(u => u.MainGroupId).ToListAsync();

            result = isFeatured.Value
                         ? result.Where(v => groupIds.Contains(v.GroupId) || !v.ToplistPosition.HasValue)
                         : result.Where(v => !groupIds.Contains(v.GroupId) && v.ToplistPosition.HasValue);
        }

        if (string.IsNullOrWhiteSpace(countryIso3) && string.IsNullOrWhiteSpace(languageIso3))
            return result;

        var cluster = (await _geoClusterProvider.DetectGeoClusters(countryIso3, languageIso3)).FirstOrDefault();

        var expr = _geoClusterProvider.BuildGeoClusterVideoMatchPredicate<VideoDto>(cluster, v => v.Country, v => v.Language);

        return result.Where(expr);
    }

    public async Task<VideoDto> GetVideoById(long id)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        return await GetVideoInfo().Where(v => v.Id == id).FirstOrDefaultAsync();
    }

    public async Task<VideoDto[]> GetVideosRemixedFromVideo(long id)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        return await GetVideoInfo().Where(v => v.RemixedFromVideoId == id).ToArrayAsync();
    }

    public async Task<ResultWithCount<ModerationCommentInfo>> GetComments(ODataQueryOptions<ModerationCommentInfo> options)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        return await _videoRepository.GetComments()
                                     .GroupJoin(
                                          _videoRepository.GetGroups(),
                                          c => c.GroupId,
                                          g => g.Id,
                                          (c, g) => new {Comment = c, Group = g}
                                      )
                                     .SelectMany(
                                          a => a.Group.DefaultIfEmpty(),
                                          (a, g) => new ModerationCommentInfo
                                                    {
                                                        Id = a.Comment.Id,
                                                        Text = a.Comment.Text,
                                                        Time = a.Comment.Time,
                                                        GroupId = a.Comment.GroupId,
                                                        VideoId = a.Comment.VideoId,
                                                        IsDeleted = a.Comment.IsDeleted,
                                                        GroupNickname = g.NickName
                                                    }
                                      )
                                     .OrderByDescending(a => a.Id)
                                     .ExecuteODataRequestWithCount(options);
    }

    public async Task<ResultWithCount<VideoReportDto>> GetVideoReportInfo(ODataQueryOptions<VideoReportDto> options)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var result = await GetVideoReportDto().ExecuteODataRequestWithCount(options);

        return result;
    }

    public async Task<VideoReportReason[]> GetVideoReportReasons()
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        return await _reportVideoRepository.GetAllVideoReportReason().ToArrayAsync();
    }

    public async Task PublishVideo(long videoId)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var videoInfo = await GetVideoShortInfo().FirstOrDefaultAsync(e => e.Id == videoId);
        if (videoInfo == null)
            return;

        if (videoInfo.IsDeleted)
            throw new AppErrorWithStatusCodeException("Can't publish deleted video", HttpStatusCode.BadRequest);

        await _videoRepository.PublishVideo(videoId);

        await _cache.DeleteKeys(VideoCacheKeys.VideoInfoKey(videoId).AllKeyVersionedCache());
    }

    public async Task UnPublishVideo(long videoId, bool includeRemixes)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var videoInfo = await GetVideoShortInfo().FirstOrDefaultAsync(e => e.Id == videoId);
        if (videoInfo == null)
            return;

        await UnPublishVideo(videoInfo, includeRemixes);
    }

    public async Task SoftDeleteVideosByHashtagId(long hashtagId, bool includeRemixes)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var ids = _videoRepository.GetVideosByHashtagId(hashtagId).Select(e => e.Id);

        var videoInfos = await GetVideoShortInfo().Where(e => ids.Contains(e.Id)).ToListAsync();

        foreach (var videoInfo in videoInfos)
            await SoftDeleteVideo(videoInfo, includeRemixes);
    }

    public async Task UnPublishVideosByHashtagId(long hashtagId, bool includeRemixes)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var ids = _videoRepository.GetVideosByHashtagId(hashtagId).Select(e => e.Id);

        var videoInfos = await GetVideoShortInfo().Where(e => ids.Contains(e.Id)).ToListAsync();

        foreach (var videoInfo in videoInfos)
            await UnPublishVideo(videoInfo, includeRemixes);
    }

    public async Task UpdateVideo(long videoId, VideoPatchRequest request)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var video = await _videoRepository.GetVideos().FirstOrDefaultAsync(e => e.Id == videoId);
        if (video == null)
            throw AppErrorWithStatusCodeException.BadRequest($"Video {videoId} is not found or not accessible", "VideoNotFound");

        if (request.IsFeatured.HasValue)
            video.ToplistPosition = request.IsFeatured.Value ? 1 : null;

        if (request.AllowRemix.HasValue)
            video.AllowRemix = request.AllowRemix.Value;

        if (request.AllowComment.HasValue)
            video.AllowComment = request.AllowComment.Value;

        if (request.StartListItem.HasValue)
            video.StartListItem = request.StartListItem <= 0 ? null : request.StartListItem;

        var anyUpdated = request.IsFeatured.HasValue || request.AllowRemix.HasValue || request.AllowComment.HasValue ||
                         request.StartListItem.HasValue;
        if (anyUpdated)
        {
            await _videoRepository.SaveChanges();

            await _cache.DeleteKeys(VideoCacheKeys.VideoInfoKey(videoId).AllKeyVersionedCache());
        }
    }

    public async Task<VideoDto> SetSoftDelete(long id, bool isDeleted, bool includeRemixes)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var videoInfo = await GetVideoShortInfo().FirstOrDefaultAsync(e => e.Id == id);
        if (videoInfo == null)
            return null;

        if (isDeleted)
        {
            await SoftDeleteVideo(videoInfo, includeRemixes);
            return await GetVideoById(id);
        }

        await _videoRepository.SetVideoDeleted(id, _currentUser, false);

        return await GetVideoById(id);
    }

    public async Task<VideoReportDto> SetVideoHidden(long incidentId, bool isHidden)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var report = await GetVideoReportDto().SingleOrDefaultAsync(v => v.Report.Id == incidentId);

        if (report == null)
            throw new AppErrorWithStatusCodeException("Incident is not found", HttpStatusCode.NotFound);

        report.Report.HideVideo = isHidden;

        await _reportVideoRepository.SaveVideoReport(report.Report);

        await _videoRepository.SetVideoDeleted(report.Video.Id, _currentUser, isHidden);

        await _cache.DeleteKeys(VideoCacheKeys.VideoInfoKey(report.Video.Id).AllKeyVersionedCache());

        return report;
    }

    public async Task SetCommentDeleted(long videoId, long commentId)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var comment = await _videoRepository.GetComments().Select(e => new {e.Id, e.IsDeleted}).FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null)
            throw new AppErrorWithStatusCodeException("Comment is not found", HttpStatusCode.NotFound);

        var isDeleted = !comment.IsDeleted;

        await _videoRepository.SetCommentDeleted(videoId, commentId, isDeleted);

        await _cache.DeleteKeysWithInfix(VideoCacheKeys.VideoKpiKey(videoId).GetKeyWithoutVersion());
    }

    public async Task<VideoReportDto> CloseIncident(long incidentId)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var report = await GetVideoReportDto().SingleOrDefaultAsync(v => v.Report.Id == incidentId);

        if (report == null)
            throw new AppErrorWithStatusCodeException("Incident is not found", HttpStatusCode.NotFound);

        if (report.Report.ClosedTime != null || report.Report.ClosedByUserId != null)
            throw new AppErrorWithStatusCodeException("Incident is already closed", HttpStatusCode.BadRequest);

        report.Report.ClosedTime = DateTime.UtcNow;
        report.Report.ClosedByUserId = _currentUser.UserId;

        await _reportVideoRepository.SaveVideoReport(report.Report);

        return report;
    }

    public async Task<VideoReportDto> ReopenIncident(long incidentId)
    {
        await _permissionService.EnsureHasVideoModerationAccess();

        var report = await GetVideoReportDto().SingleOrDefaultAsync(v => v.Report.Id == incidentId);

        if (report == null)
            throw new AppErrorWithStatusCodeException("Incident is not found", HttpStatusCode.NotFound);

        if (report.Report.ClosedTime == null || report.Report.ClosedByUserId == null)
            throw new AppErrorWithStatusCodeException("Incident is not yet closed", HttpStatusCode.BadRequest);

        report.Report.ClosedTime = null;
        report.Report.ClosedByUserId = null;

        await _reportVideoRepository.SaveVideoReport(report.Report);

        return report;
    }

    public async Task HardDeleteAccountData(long groupId)
    {
        await _hardDeleteAccountDataHelper.HardDeleteAccountData(groupId);
    }

    public async Task<VideoContentInfo> ToVideoContentInfo(VideoDto video)
    {
        var cookies = await CreateSignedCookie(video);

        var sharingUrl = CreateSignedUrl(_videoNamingHelper.GetSharingVideoUrl(video));

        return new VideoContentInfo
               {
                   Id = video.Id,
                   GroupId = video.GroupId,
                   RedirectUrl = _videoNamingHelper.GetVideoUrl(video),
                   SharingUrl = sharingUrl,
                   PlayerUrl = sharingUrl,
                   ThumbnailUrl = CreateSignedUrl(_videoNamingHelper.GetVideoThumbnailUrl(video)),
                   SingleFileVideoUrl = CreateSignedUrl(_videoNamingHelper.GetSharingVideoUrl(video)),
                   SignedCookies = new Dictionary<string, string>
                                   {
                                       {cookies.Policy.Key, cookies.Policy.Value},
                                       {cookies.Signature.Key, cookies.Signature.Value},
                                       {cookies.KeyPairId.Key, cookies.KeyPairId.Value}
                                   }
               };
    }

    public async Task<VideoDto[]> WithAiContent(VideoDto[] source)
    {
        var ids = source.Where(c => c.AiGeneratedContentId != null).Select(s => s.AiGeneratedContentId).ToHashSet();

        var aiContent = await _aiAdminService.GetAiGeneratedContent().Where(aic => ids.Contains(aic.Id)).ToDictionaryAsync(aic => aic.Id);

        foreach (var content in aiContent.Values)
            await _aiAdminService.InitUrls(content);

        foreach (var video in source.Where(v => v.AiGeneratedContentId != null))
            if (aiContent.TryGetValue(video.AiGeneratedContentId!.Value, out var content))
                video.AiGeneratedContent = content;

        return source;
    }

    private async Task UnPublishVideo(VideoShortDto video, bool includeRemixes)
    {
        await _videoRepository.UnPublishVideo(video.Id);

        if (includeRemixes)
        {
            var remixes = await GetVideoShortInfo().Where(v => v.RemixedFromVideoId == video.Id).Where(v => !v.IsDeleted).ToArrayAsync();

            foreach (var remix in remixes)
            {
                await _videoRepository.UnPublishVideo(remix.Id);

                await _cache.DeleteKeys(VideoCacheKeys.VideoInfoKey(remix.Id).AllKeyVersionedCache());
            }
        }

        await _cache.DeleteKeys(VideoCacheKeys.VideoInfoKey(video.Id).AllKeyVersionedCache());
    }

    private async Task SoftDeleteVideo(VideoShortDto video, bool includeRemixes)
    {
        await _videoRepository.SetVideoDeleted(video.Id, _currentUser, true);

        await _cache.DeleteKeys(VideoCacheKeys.VideoInfoKey(video.Id).AllKeyVersionedCache());

        if (includeRemixes && video.LevelId != null)
        {
            var remixes = await GetVideoShortInfo().Where(v => v.RemixedFromVideoId == video.Id).Where(v => !v.IsDeleted).ToArrayAsync();

            foreach (var remix in remixes)
            {
                await _videoRepository.SetVideoDeleted(remix.Id, _currentUser, true);

                await _cache.DeleteKeys(VideoCacheKeys.VideoInfoKey(remix.Id).AllKeyVersionedCache());
            }
        }

        await _notificationAddingService.NotifyVideoDeleted(
            new NotifyVideoDeletedMessage {VideoId = video.Id, CurrentGroupId = _currentUser.UserMainGroupId}
        );
    }

    private IQueryable<VideoDto> GetVideoInfo()
    {
        return _videoRepository.GetVideos()
                               .GroupJoin(_videoRepository.GetVideoKpi(), v => v.Id, k => k.VideoId, (v, k) => new {v, k})
                               .SelectMany(kpi => kpi.k.DefaultIfEmpty(), (v, k) => new {v.v, k})
                               .AsSplitQuery()
                               .Select(
                                    v => new VideoDto
                                         {
                                             LevelId = v.v.LevelId,
                                             Size = v.v.Size,
                                             CreatedTime = v.v.CreatedTime,
                                             Duration = v.v.Duration,
                                             GroupId = v.v.GroupId,
                                             GroupNickName = v.v.Group.NickName,
                                             RemixedFromVideoId = v.v.RemixedFromVideoId,
                                             RemixedFromLevelId = v.v.RemixedFromVideo.LevelId,
                                             Songs = v.v.SongInfo,
                                             UserSounds = v.v.UserSoundInfo,
                                             CharactersCount = v.v.CharactersCount,
                                             Id = v.v.Id,
                                             OriginalCreatorGroupNickName =
                                                 v.v.RemixedFromVideo == null ? null : v.v.RemixedFromVideo.Group.NickName,
                                             OriginalCreatorGroupId =
                                                 v.v.RemixedFromVideo == null ? null : v.v.RemixedFromVideo.GroupId,
                                             ToplistPosition = v.v.ToplistPosition,
                                             IsRemixable = v.v.IsRemixable,
                                             TemplateIds = v.v.TemplateIds,
                                             IsDeleted = v.v.IsDeleted,
                                             DeletedByGroupId = v.v.DeletedByGroupId,
                                             Version = v.v.Version,
                                             Access = v.v.Access,
                                             AllowRemix = v.v.AllowRemix,
                                             AllowComment = v.v.AllowComment,
                                             SchoolTaskId = v.v.SchoolTaskId,
                                             StartListItem = v.v.StartListItem,
                                             Language = v.v.Language,
                                             Country = v.v.Country,
                                             ExternalSongIds = v.v.ExternalSongIds,
                                             PublishTypeId = v.v.PublishTypeId,
                                             ThumbnailUrl = GetThumbnailUrl(v.v),
                                             Kpi =
                                                 new VideoKpiInfo
                                                 {
                                                     VideoId = v.v.Id,
                                                     Comments = v.k == null ? 0 : v.k.Comments,
                                                     Likes = v.k == null ? 0 : v.k.Likes,
                                                     Remixes = v.k == null ? 0 : v.k.Remixes,
                                                     Shares = v.k == null ? 0 : v.k.Shares,
                                                     Views = v.k == null ? 0 : v.k.Views,
                                                     BattlesLost = v.k == null ? 0 : v.k.BattlesLost,
                                                     BattlesWon = v.k == null ? 0 : v.k.BattlesWon,
                                                     EngagementRate =
                                                         v.k == null || v.k.Views == 0
                                                             ? 0
                                                             : (v.k.Comments + v.k.Likes + v.k.Shares) / v.k.Views * 100
                                                 },
                                             Description = v.v.Description,
                                             Hashtags =
                                                 v.v.VideoAndHashtag.Select(
                                                       e => new HashtagInfo
                                                            {
                                                                Id = e.Hashtag.Id,
                                                                Name = e.Hashtag.Name,
                                                                ViewsCount = e.Hashtag.ViewsCount,
                                                                UsageCount = e.Hashtag.VideoCount
                                                            }
                                                   )
                                                  .ToArray(),
                                             Mentions =
                                                 v.v.VideoMentions.Select(
                                                       e => new TaggedGroup
                                                            {
                                                                GroupId = e.GroupId, GroupNickname = e.Group.NickName
                                                            }
                                                   )
                                                  .ToArray(),
                                             TaggedGroups =
                                                 v.v.VideoGroupTags.Select(
                                                       t => new TaggedGroup
                                                            {
                                                                GroupId = t.GroupId, GroupNickname = t.Group.NickName
                                                            }
                                                   )
                                                  .ToArray(),
                                             AiGeneratedContentId = v.v.AiContentId
                                         }
                                );
    }

    private Task<FreverAmazonCloudFrontCookiesForCustomPolicy> CreateSignedCookie(IVideoNameSource video)
    {
        var resourcePath = _videoNamingHelper.GetSignedCookieResourcePath(video);

        return Task.Run(
            () => FreverAmazonCloudFrontSigner.GetCookiesForCustomPolicy(
                resourcePath,
                _cloudFrontConfiguration.CloudFrontCertKeyPairId,
                DateTime.Now.AddMinutes(_cloudFrontConfiguration.CloudFrontSignedCookieLifetimeMinutes),
                DateTime.Now - new TimeSpan(
                    0,
                    1,
                    0
                ), //needed to remove at least few seconds, because there was the issue when client start use this cookies immediately
                null
            )
        );
    }

    private string CreateSignedUrl(string url)
    {
        return FreverAmazonCloudFrontSigner.SignUrlCanned(url, _cloudFrontConfiguration.CloudFrontCertKeyPairId, DateTime.Now.AddDays(10));
    }

    private static string GetThumbnailUrl(IVideoNameSource video)
    {
        return FreverAmazonCloudFrontSigner.SignUrlCanned(
            _staticNamingHelper.GetVideoThumbnailUrl(video),
            _staticConfig.CloudFrontCertKeyPairId,
            DateTime.Now.AddDays(10)
        );
    }

    private IQueryable<VideoShortDto> GetVideoShortInfo()
    {
        return _videoRepository.GetVideos()
       .Select(
            e => new VideoShortDto
                 {
                     Id = e.Id,
                     LevelId = e.LevelId,
                     SchoolTaskId = e.SchoolTaskId,
                     RemixedFromVideoId = e.RemixedFromVideoId,
                     IsDeleted = e.IsDeleted,
                     Access = e.Access
                 }
        );
    }

    private IQueryable<VideoReportDto> GetVideoReportDto()
    {
        var videos = GetVideoInfo();

        var reports = _reportVideoRepository.AllVideoReports();

        return reports.Join(videos, r => r.VideoId, v => v.Id, (r, v) => new VideoReportDto {Report = r, Video = v});
    }
}