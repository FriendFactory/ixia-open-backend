using Frever.Client.Shared.Files;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.DataAccess;

namespace NotificationService.Core;

public static class Configuration
{
    public static void AddNotificationServices(this IServiceCollection services)
    {
        services.AddEntityFiles();
        services.AddEntityFileConfiguration<AiGeneratedImageFileConfig>();
        services.AddEntityFileConfiguration<AiGeneratedVideoFileConfig>();

        services.AddNotificationDataAccess();
        services.AddScoped<INotificationReadService, NotificationReadService>();
        services.AddScoped<INotificationAddingService, NotificationAddingService>();
        services.AddScoped<IMainServerService, MainServerService>();
        services.AddScoped<IVideoServerService, VideoServerService>();
        services.AddScoped<INotificationMapper, NotificationMapper>();
        services.AddSingleton<IPushNotificationSender, OneSignalPushNotificationSender>();
    }
}