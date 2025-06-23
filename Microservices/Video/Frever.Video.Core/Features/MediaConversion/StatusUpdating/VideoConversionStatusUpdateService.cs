using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Features.Caching;
using Frever.Video.Core.Features.MediaConversion.DataAccess;
using Frever.Video.Core.Features.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService;
using NotificationService.Client.Messages;

#pragma warning disable CS8603

namespace Frever.Video.Core.Features.MediaConversion.StatusUpdating;

public class VideoConversionStatusUpdateService(
    IVideoStatusUpdateRepository repo,
    INotificationAddingService notificationAddingService,
    ILoggerFactory loggerFactory,
    IVideoCachingService videoCachingService
) : IVideoConversionStatusUpdateService
{
    private readonly ILogger _log = loggerFactory.CreateLogger("Frever.VideoConversionStatusUpdater");

    public async Task<Frever.Shared.MainDb.Entities.Video> HandleVideoConversionCompletion(long videoId, VideoConversionType conversionType)
    {
        _log.LogInformation("Video file {VideoId} converted, updating db...", videoId);
        var video = await repo.UnsecureGetVideoIncludingDeletedById(videoId).FirstOrDefaultAsync();

        if (video == null)
        {
            _log.LogError("Video {VideoId} is not found", videoId);
            return null;
        }

        video.ConversionStatus |= conversionType == VideoConversionType.Video
                                      ? VideoConversion.VideoConverted
                                      : VideoConversion.ThumbnailConverted;

        if (video.ConversionStatus != VideoConversion.Completed)
        {
            await repo.SaveChanges();
            return video;
        }

        await repo.ClearDeletionMarkFromVideo(video.Id);
        _log.LogInformation("Conversion mark for video id={VideoId} cleared", video.Id);

        await notificationAddingService.NotifyNewVideo(
            new NotifyNewVideoMessage {VideoId = video.Id, CurrentGroupId = video.GroupId, IsVideoConversionPerformed = true}
        );

        await videoCachingService.DeleteVideoDetailsCache(video.Id);
        _log.LogInformation("Processing of video id={VideoId} completed", videoId);

        return video;
    }
}