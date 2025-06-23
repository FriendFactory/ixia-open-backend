using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServer.Permissions.Sub13;
using AuthServerShared;
using Common.Infrastructure;
using Common.Infrastructure.ModerationProvider;
using FluentValidation;
using Frever.Client.Shared.Social.Services;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Comments.DataAccess;
using Frever.Video.Core.Features.Shared;
using Frever.Videos.Shared.CachedVideoKpis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationService;
using NotificationService.Client.Messages;

namespace Frever.Video.Core.Features.Comments;

public class CommentModificationService(
    ICommentModificationRepository repo,
    UserInfo currentUser,
    IUserPermissionService userPermissionService,
    ISocialSharedService socialSharedService,
    IParentalConsentValidationService parentalConsent,
    IValidator<AddCommentRequest> addCommentValidator,
    IModerationProviderApi moderationProviderApi,
    ILogger<CommentModificationService> log,
    IMentionService mentionService,
    IUserCommentInfoProvider userCommentInfoProvider,
    INotificationAddingService notificationAddingService,
    IVideoKpiCachingService kpiCache,
    IOneVideoAccessor oneVideoAccessCheck
) : ICommentModificationService
{
    public async Task<UserCommentInfo> AddComment(long videoId, AddCommentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        await userPermissionService.EnsureCurrentUserActive();
        await parentalConsent.EnsureCommentsAllowed();

        await addCommentValidator.ValidateAndThrowAsync(request);

        if (!await oneVideoAccessCheck.IsVideoAccessibleTo(
                 FetchVideoInfoFrom.WriteDb,
                 currentUser,
                 videoId,
                 v => v.GroupId == currentUser || v.AllowComment
             ))
            throw new AppErrorWithStatusCodeException(
                "Video is not found or not available or comments not allowed",
                HttpStatusCode.NotFound
            );

        var moderationResult = await moderationProviderApi.CallModerationProviderApiText(request.Text);

        if (!moderationResult.PassedModeration)
        {
            log.LogInformation(
                "Comment {Comment} doesn't pass moderation: {Reason} {Error}",
                request.Text,
                moderationResult.Reason,
                moderationResult.ErrorMessage
            );
            throw AppErrorWithStatusCodeException.BadRequest(moderationResult.Reason, "This comment is inappropriate.");
        }

        var comment = new Comment
                      {
                          Text = request.Text,
                          GroupId = currentUser,
                          VideoId = videoId,
                          Time = DateTime.UtcNow,
                          Mentions = await mentionService.GetMentions(request.Text),
                          ReplyToCommentId = request.ReplyToCommentId
                      };

        await repo.AddComment(comment);

        var newComment = await GetCommentById(videoId, comment.Id);

        await kpiCache.UpdateVideoKpi(videoId, kpi => kpi.Comments, 1);

        await notificationAddingService.NotifyNewCommentOnVideo(
            new NotifyNewCommentOnVideoMessage
            {
                CommentedBy = currentUser,
                CommentId = comment.Id,
                VideoId = videoId,
                ReplyToCommentId = comment.ReplyToCommentId
            }
        );

        foreach (var mention in comment.Mentions.Where(m => m.GroupId != currentUser))
            await notificationAddingService.NotifyNewMentionsInCommentOnVideo(
                new NotifyNewMentionInCommentOnVideoMessage
                {
                    CommentedBy = currentUser,
                    CommentId = comment.Id,
                    VideoId = videoId,
                    MentionedGroupId = mention.GroupId
                }
            );

        return newComment;
    }

    public async Task<UserCommentInfo> LikeComment(long videoId, long commentId)
    {
        await userPermissionService.EnsureCurrentUserActive();
        await parentalConsent.EnsureCommentsAllowed();

        var comment = await repo.GetVideoComments(videoId).SingleOrDefaultAsync(c => c.Id == commentId);
        if (comment == null)
            return null;

        await using var transaction = await repo.BeginTransaction();

        var like = await repo.GetCommentLike(videoId, commentId, currentUser).SingleOrDefaultAsync();

        if (like == null)
        {
            await repo.AddCommentLike(
                new CommentLike
                {
                    VideoId = videoId,
                    CommentId = commentId,
                    Time = DateTime.UtcNow,
                    GroupId = currentUser
                }
            );

            await repo.IncrementCommentLikeCount(videoId, commentId);
        }

        await transaction.Commit();

        return await GetCommentById(videoId, commentId);
    }

    public async Task<UserCommentInfo> UnlikeComment(long videoId, long commentId)
    {
        await userPermissionService.EnsureCurrentUserActive();
        await parentalConsent.EnsureCommentsAllowed();

        var comment = await repo.GetVideoComments(videoId).SingleOrDefaultAsync(c => c.Id == commentId);
        if (comment == null)
            return null;

        await using var transaction = await repo.BeginTransaction();

        if (await repo.RemoveCommentLike(videoId, commentId, currentUser) == 1)
            await repo.DecrementCommentLikeCount(videoId, commentId);

        await transaction.Commit();

        return await GetCommentById(videoId, commentId);
    }

    public Task<UserCommentInfo> PinComment(long videoId, long commentId)
    {
        return SetCommentPinned(videoId, commentId, true);
    }

    public Task<UserCommentInfo> UnPinComment(long videoId, long commentId)
    {
        return SetCommentPinned(videoId, commentId, false);
    }

    private async Task<UserCommentInfo> SetCommentPinned(long videoId, long commentId, bool isPinned)
    {
        await userPermissionService.EnsureCurrentUserActive();
        await parentalConsent.EnsureCommentsAllowed();

        var video = await repo.GetGroupVideo(currentUser).SingleOrDefaultAsync(e => e.Id == videoId);
        if (video == null || video.IsDeleted)
            throw new ArgumentException("Video is not found or user is not authorized to perform the operation");

        await repo.SetCommentPinned(videoId, commentId, isPinned);

        return await GetCommentById(videoId, commentId);
    }

    private async Task<UserCommentInfo> GetCommentById(long videoId, long commentId)
    {
        await userPermissionService.EnsureCurrentUserActive();

        if (!await repo.GetVideo(videoId).AnyAsync())
            return null;

        var blockedGroups = await socialSharedService.GetBlocked(currentUser.UserMainGroupId);

        var comments = await userCommentInfoProvider.MakeUserCommentsInfo(
                           repo.GetVideoComments(videoId).Where(q => q.Id == commentId),
                           repo.GetCommentLike(videoId, commentId, currentUser),
                           repo.GetCommentGroupInfo(videoId),
                           blockedGroups
                       );

        return comments.FirstOrDefault();
    }
}