using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using Common.Infrastructure.EmailSending;
using FluentValidation;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Video.Core.Features.ReportInappropriate.DataAccess;
using Frever.Video.Core.Features.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.ReportInappropriate;

internal sealed class ReportInappropriateVideoService(
    ILogger<ReportInappropriateVideoService> log,
    IValidator<ReportInappropriateVideoRequest> validator,
    IEmailSendingService emailSendingService,
    VideoServerOptions options,
    UserInfo currentUser,
    IOneVideoAccessor oneVideoAccessCheck,
    IReportInappropriateVideoRepository repo
) : IReportInappropriateVideoService
{
    public Task<VideoReportReason[]> GetVideoReportReasons()
    {
        return repo.GetAllVideoReportReason().ToArrayAsync();
    }

    public async Task<VideoReport> ReportVideo(ReportInappropriateVideoRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        log.LogInformation("Report video {RequestVideoId} by {CurrentUserId}", request.VideoId, currentUser.UserMainGroupId);

        await validator.ValidateAsync(request);

        if (!await oneVideoAccessCheck.IsVideoAccessibleTo(FetchVideoInfoFrom.WriteDb, currentUser, request.VideoId))
            throw new InvalidOperationException($"Video {request.VideoId} is not found or not available");

        var reason = await repo.GetVideoReportReason(request.ReasonId);

        var videoReport = new VideoReport
                          {
                              Message = request.Message,
                              ReasonId = request.ReasonId,
                              VideoId = request.VideoId,
                              HideVideo = false,
                              ReporterGroupId = currentUser
                          };

        await repo.SaveVideoReport(videoReport);

        await SendReportEmail(request.VideoId, request.Message, reason.Name);

        return videoReport;
    }

    private Task SendReportEmail(long videoId, string message, string reason)
    {
        var emailMessage = string.Join(
            Environment.NewLine,
            $"Video {videoId} is reported by {currentUser.UserMainGroupId}",
            $"Reason {reason}",
            "Message: ",
            "",
            message
        );

        return emailSendingService.SendEmail(
            new SendEmailParams
            {
                Body = emailMessage,
                Subject = $"New video report: video {videoId} is reported by {currentUser.UserMainGroupId}",
                To = [options.VideoReportNotificationEmail]
            }
        );
    }
}