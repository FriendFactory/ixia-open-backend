using System;
using Frever.Videos.Shared.CachedVideoKpis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Shared.ActivityRecording;

public static class ServiceConfiguration
{
    public static void AddUserActivityRecording(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<IUserActivityRecordingService, UserActivityRecordingService>();
        services.AddScoped<IGroupActivityRepository, PersistentGroupActivityRepository>();

        services.AddCachedVideoKpis();
    }
}