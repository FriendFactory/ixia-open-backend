using System;
using FluentValidation;
using Frever.Video.Core.Features.Comments.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.Comments;

public static class ServiceConfiguration
{
    public static void AddVideoComments(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICommentReadingService, CommentReadingService>();
        services.AddScoped<ICommentModificationService, CommentModificationService>();

        services.AddScoped<IMentionService, MentionService>();
        services.AddScoped<IValidator<AddCommentRequest>, AddCommentRequestValidator>();
        services.AddScoped<IUserCommentInfoProvider, UserCommentInfoProvider>();

        services.AddScoped<IMentionRepository, PersistentMentionRepository>();
        services.AddScoped<ICommentReadingRepository, PersistentCommentReadingRepository>();
        services.AddScoped<ICommentModificationRepository, PersistentCommentModificationRepository>();
    }
}