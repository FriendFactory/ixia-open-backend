using System;
using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.Video.Core.Features.Hashtags.DataAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.Hashtags.Workers;

internal sealed class HashtagViewsCountSynchronizerBackgroundWorker(IServiceProvider serviceProvider) : BackgroundJobBase(serviceProvider)
{
    protected override TimeSpan RunInterval { get; } = TimeSpan.FromHours(1);

    protected override async Task Run(IServiceScope scope)
    {
        var service = scope.ServiceProvider.GetRequiredService<IHashtagStatsUpdaterRepository>();
        await service.RefreshHashtagViewsCountAsync();
        await service.RefreshHashtagsVideoCountAsync();
    }
}