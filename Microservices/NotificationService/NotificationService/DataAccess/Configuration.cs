using Microsoft.Extensions.DependencyInjection;

namespace NotificationService.DataAccess;

public static class Configuration
{
    public static void AddNotificationDataAccess(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository, EntityFrameworkNotificationRepository>();
    }
}