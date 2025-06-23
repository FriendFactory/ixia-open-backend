using System;
using Frever.Cache.Configuration;
using Frever.Cache.Strategies;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Video.Core.Features.CreatePage;

public static class ServiceConfiguration
{
    public static void AddCreatePage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ICreatePageRepository, CreatePageRepository>();
        services.AddScoped<ICreatePageService, CreatePageService>();

        services.AddEntityFiles();
        services.AddEntityFileConfiguration<AiGeneratedImageFileConfig>();

        services.AddFreverCaching(o => { o.InMemoryDoubleCache.Blob<CreatePageContent>(SerializeAs.Json, false, typeof(ContentRow)); });
    }
}