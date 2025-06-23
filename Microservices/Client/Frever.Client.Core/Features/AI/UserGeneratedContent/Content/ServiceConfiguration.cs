using System;
using FluentValidation;
using Frever.Client.Core.Features.AI.Generation.StatusUpdating;
using Frever.Client.Core.Features.AI.Metadata;
using Frever.Client.Core.Features.AI.Moderation;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Core;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Core.Validators;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Data;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.Client.Core.Features.MediaFingerprinting;
using Frever.Client.Core.Features.Sounds;
using Frever.Client.Shared.Files;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content;

public static class ServiceConfiguration
{
    public static void AddAiGeneratedContent(this IServiceCollection services, MediaFingerprintingOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEntityFiles();
        services.AddEntityFileConfiguration<AiGeneratedImageFileConfig>();
        services.AddEntityFileConfiguration<AiGeneratedImagePersonFileConfig>();
        services.AddEntityFileConfiguration<AiGeneratedImageSourceFileConfig>();
        services.AddEntityFileConfiguration<AiGeneratedVideoFileConfig>();
        services.AddEntityFileConfiguration<AiGeneratedVideoClipFileConfig>();

        // Validators
        services.AddScoped<IValidator<AiGeneratedContentInput>, AiGeneratedContentInputValidator>();
        services.AddScoped<IValidator<AiGeneratedImageInput>, AiGeneratedImageInputValidator>();
        services.AddScoped<IValidator<AiGeneratedVideoInput>, AiGeneratedVideoInputValidator>();
        services.AddScoped<IValidator<AiGeneratedImagePersonInput>, AiGeneratedImagePersonInputValidator>();
        services.AddScoped<IValidator<AiGeneratedImageSourceInput>, AiGeneratedImageSourceInputValidator>();
        services.AddScoped<IValidator<AiGeneratedVideoClipInput>, AiGeneratedVideoClipInputValidator>();

        services.AddScoped<IAiGeneratedContentRepository, PersistentAiGeneratedContentRepository>();
        services.AddScoped<IAiGeneratedContentService, AiGeneratedContentService>();

        services.AddAiMetadata();
        services.AddAiContentModeration();
        services.AddScoped<IPollingJobManager, RedisPollingJobManager>();

        if (options != null)
            services.AddMediaFingerprinting(options);

        services.AddSounds(new AssetServerSettings {NewAssetDays = 9});
    }
}