using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Views.DataAccess;
using Frever.Videos.Shared.CachedVideoKpis;

namespace Frever.Video.Core.Features.Views;

public interface IVideoViewRecorder
{
    Task RecordVideoView(ViewViewInfo[] views);
}

public class PersistentVideoViewRecorder(
    IUserPermissionService userPermissionService,
    UserInfo currentUser,
    IVideoKpiCachingService kpiCachingService,
    IRecordVideoViewRepository repo
) : IVideoViewRecorder
{
    public async Task RecordVideoView(ViewViewInfo[] views)
    {
        await userPermissionService.EnsureCurrentUserActive();

        if (views == null || views.Length == 0)
            return;

        var videoViews = views.Select(
            v => new VideoView
                 {
                     VideoId = v.VideoId,
                     Time = v.ViewDate.ToUniversalTime(),
                     UserId = currentUser.UserId,
                     FeedTab = v.FeedTab,
                     FeedType = v.FeedType
                 }
        );

        await repo.AppendVideoView(videoViews);

        foreach (var view in views)
            await kpiCachingService.UpdateVideoKpi(view.VideoId, kpi => kpi.Views, 1);
    }
}