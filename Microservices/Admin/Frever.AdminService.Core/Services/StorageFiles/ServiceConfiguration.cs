using System;
using FluentValidation;
using Frever.AdminService.Core.Services.StorageFiles.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.StorageFiles;

public static class ServiceConfiguration
{
    public static void AddStorageFiles(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IStorageFileService, StorageFileService>();
        services.AddScoped<IValidator<UploadStorageFileModel>, UploadStorageFileModelValidator>();
    }
}