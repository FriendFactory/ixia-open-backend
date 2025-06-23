using System;
using Frever.Client.Core.Features.AI.Moderation.Core;
using Frever.Client.Core.Features.AI.Moderation.External.OpenAi;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.AI.Moderation;

public static class ServiceConfiguration
{
    public static void AddAiContentModeration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient();
        services.AddScoped<IOpenAiClient, HttpOpenAiClient>();
        services.AddScoped<IAiContentModerationService, OpenAiContentModerationService>();
    }
}