using System;
using Frever.Client.Shared.AI.Metadata.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Shared.AI.Metadata;

public static class ServiceConfiguration
{
    public static void AddAiWorkflowMetadata(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAiWorkflowMetadataRepository, PersistentAiWorkflowMetadataRepository>();
        services.AddScoped<IAiWorkflowMetadataService, AiWorkflowMetadataService>();
    }
}