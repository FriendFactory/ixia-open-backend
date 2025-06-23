using System;
using FluentValidation;
using Frever.AdminService.Core.Services.InAppPurchases.Contracts;
using Frever.AdminService.Core.Services.InAppPurchases.DataAccess;
using Frever.AdminService.Core.Services.InAppPurchases.OfferGenerationInfo;
using Frever.AdminService.Core.Services.InAppPurchases.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Core.Services.InAppPurchases;

public static class ServiceConfiguration
{
    public static void AddInAppPurchaseAdmin(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IInAppOfferGenerationInfoService, InAppOfferGenerationInfoService>();
        services.AddScoped<IInAppPurchaseRepository, PersistentInAppPurchaseRepository>();
        services.AddScoped<IInAppPurchaseService, InAppPurchaseService>();
        services.AddScoped<IValidator<InAppProductDetailsDto>, InAppProductDetailsDtoValidator>();
        services.AddScoped<IValidator<InAppProductPriceTierDto>, InAppProductPriceTierDtoValidator>();
        services.AddScoped<IValidator<InAppProductShortDto>, InAppProductShortDtoValidator>();
        services.AddScoped<IValidator<HardCurrencyExchangeOfferDto>, HardCurrencyExchangeOfferDtoValidator>();
    }
}