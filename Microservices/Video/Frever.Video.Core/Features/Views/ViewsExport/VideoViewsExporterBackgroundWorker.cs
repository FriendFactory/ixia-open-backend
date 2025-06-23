using System;
using System.Threading.Tasks;
using Common.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.Views.ViewsExport;

public class VideoViewsExporterBackgroundWorker(IServiceProvider serviceProvider) : BackgroundJobBase(serviceProvider)
{
    protected override TimeSpan RunInterval { get; } = TimeSpan.FromHours(24);

    protected override async Task Run(IServiceScope scope)
    {
        var service = scope.ServiceProvider.GetRequiredService<IVideoViewsExportService>();
        await service.DoExport();
    }
}