using Frever.Video.Core.Features.Views.DataAccess;
using Frever.Video.Core.Features.Views.ViewsExport;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.Views;

public static class ServiceConfiguration
{
    public static void AddVideoViewsFeatures(this IServiceCollection services, string bucketName)
    {
        var s3ExporterOptions = new AwsS3ViewsExporterOptions {S3Bucket = bucketName};
        s3ExporterOptions.Validate();

        services.AddSingleton(s3ExporterOptions);
        services.AddScoped<IVideoViewsExportService, AwsS3VideoViewsExportService>();

        services.AddScoped<IRecordVideoViewRepository, PersistentRecordVideoViewRepository>();
        services.AddScoped<IVideoViewRecorder, PersistentVideoViewRecorder>();

        services.AddHostedService<VideoViewsExporterBackgroundWorker>();
    }
}