using System;
using Amazon.S3.Transfer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Frever.Client.Shared.Files;

public static class ServiceConfiguration
{
    public static void AddEntityFiles(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<TransferUtility>();

        services.TryAddScoped<IFileStorageService, FileStorageService>();
        services.TryAddScoped<IAdvancedFileStorageService, FileStorageService>();
        services.TryAddScoped<IFileStorageBackend, AwsS3FileStorageBackend>();
        services.TryAddScoped<IFileUploaderFactory, FileUploaderFactory>();
        services.TryAddTransient<IFileUploader, ParallelFileUploader>();
        services.TryAddSingleton<IExternalFileDownloader, ExternalFileDownloader>();
    }

    public static void AddEntityFileConfiguration<TFileConfig>(this IServiceCollection services)
        where TFileConfig : class, IEntityFileMetadataConfiguration
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEntityFiles();
        services.AddScoped<IEntityFileMetadataConfiguration, TFileConfig>();
    }
}