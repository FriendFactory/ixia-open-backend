using Microsoft.Extensions.DependencyInjection;

namespace Common.Infrastructure.Aws;

public static class AwsRetryExtensions
{
    public static void AddAwsRetryHelpers(this IServiceCollection services)
    {
        services.AddSingleton<IAwsRetryHelper, AwsRetryHelper>();
    }
}