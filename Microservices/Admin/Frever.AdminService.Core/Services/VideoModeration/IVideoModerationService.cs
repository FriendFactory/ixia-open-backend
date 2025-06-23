using System.Linq;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.VideoModeration.Contracts;
using Frever.AdminService.Core.Utils;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Microsoft.AspNet.OData.Query;

namespace Frever.AdminService.Core.Services.VideoModeration;

public interface IVideoModerationService
{
    Task<IQueryable<VideoDto>> GetAllVideos(bool? isFeatured, VideoAccess? access, string countryIso3, string languageIso3);

    Task<ResultWithCount<ModerationCommentInfo>> GetComments(ODataQueryOptions<ModerationCommentInfo> options);

    Task<ResultWithCount<VideoReportDto>> GetVideoReportInfo(ODataQueryOptions<VideoReportDto> options);

    Task<VideoReportReason[]> GetVideoReportReasons();

    Task<VideoDto> GetVideoById(long id);

    Task<VideoDto[]> GetVideosRemixedFromVideo(long id);

    Task<VideoContentInfo> ToVideoContentInfo(VideoDto video);

    Task PublishVideo(long videoId);

    Task UnPublishVideo(long videoId, bool includeRemixes);

    Task UpdateVideo(long videoId, VideoPatchRequest request);

    Task<VideoDto> SetSoftDelete(long id, bool isDeleted, bool includeRemixes);

    Task<VideoReportDto> SetVideoHidden(long incidentId, bool isHidden);

    Task SetCommentDeleted(long videoId, long commentId);

    Task<VideoReportDto> CloseIncident(long incidentId);

    Task<VideoReportDto> ReopenIncident(long incidentId);

    Task HardDeleteAccountData(long groupId);

    Task SoftDeleteVideosByHashtagId(long hashtagId, bool includeRemixes);

    Task UnPublishVideosByHashtagId(long hashtagId, bool includeRemixes);
    Task<VideoDto[]> WithAiContent(VideoDto[] source);
}