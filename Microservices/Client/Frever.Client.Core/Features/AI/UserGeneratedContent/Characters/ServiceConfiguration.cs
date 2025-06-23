using System;
using FluentValidation;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Characters.Contracts;
using Frever.Client.Shared.Files;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Characters;

public static class ServiceConfiguration
{
    public static void AddAiCharacters(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddEntityFiles();
        services.AddEntityFileConfiguration<AiCharacterImageFileConfig>();

        services.AddScoped<IAiCharacterRepository, AiCharacterRepository>();
        services.AddScoped<IAiCharacterService, AiCharacterService>();
        services.AddScoped<IValidator<AiCharacterInput>, AiCharacterValidator>();
        services.AddScoped<IValidator<AiCharacterImageInput>, AiCharacterImageValidator>();
    }
}