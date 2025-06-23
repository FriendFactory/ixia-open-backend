using System;
using FluentValidation;
using Frever.Video.Core.Features.Uploading.DataAccess;
using Frever.Video.Core.Features.Uploading.Models;
using Frever.Video.Core.Features.Uploading.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.Uploading;

public static class ServiceConfiguration
{
    public static void AddVideoUploading(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IVideoUploadingRepository, PersistentVideoUploadingRepository>();
        services.AddScoped<IVideoUploadService, VideoUploadService>();

        services.AddScoped<IValidator<CompleteNonLevelVideoUploadingRequest>, CompleteNonLevelVideoUploadingRequestValidator>();
    }
}