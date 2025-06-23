using System.Threading.Tasks;

namespace Frever.Client.Shared.ActivityRecording;

public interface IUserActivityRecordingService
{
    Task OnVideoLike(long videoId, long groupId);

    Task OnLogin();

    Task OnPublishedVideoShare(long videoId);
}