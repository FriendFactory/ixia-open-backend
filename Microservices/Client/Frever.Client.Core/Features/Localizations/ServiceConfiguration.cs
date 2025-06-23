using System;
using System.Collections.Generic;
using Frever.Cache.Configuration;
using Frever.Cache.Strategies;
using Frever.ClientService.Contract.Locales;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.Localizations;

public static class ServiceConfiguration
{
    public static void AddLocalizations(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ILocalizationRepository, PersistentLocalizationRepository>();
        services.AddScoped<ILocalizationService, LocalizationService>();

        services.AddFreverCaching(
            options =>
            {
                options.InMemory.Blob<CountryDto[]>(SerializeAs.Protobuf, false, typeof(Country));
                options.InMemory.Blob<LanguageDto[]>(SerializeAs.Protobuf, false, typeof(Language));
                options.InMemory.Blob<LocalizationInfo[]>(SerializeAs.Protobuf, false, typeof(Localization));
                options.InMemory.Blob<List<LocalizationInternal>>(SerializeAs.Protobuf, false, typeof(Localization));
            }
        );
    }
}