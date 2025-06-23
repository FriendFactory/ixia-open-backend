using System;
using FluentValidation;
using Frever.Client.Shared.Files;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.AI;

public static class ServiceConfiguration
{
    public static void AddAi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEntityFiles();
        services.AddEntityFileConfiguration<ArtStyleFileConfig>();

        services.AddScoped<IMetadataService, MetadataService>();
        services.AddScoped<IValidator<AiArtStyle>, AiArtStyleValidator>();
        services.AddScoped<IValidator<AiLlmPrompt>, AiLlmPromptValidator>();
        services.AddScoped<IValidator<AiWorkflowMetadata>, AiWorkflowMetadataValidator>();
    }
}