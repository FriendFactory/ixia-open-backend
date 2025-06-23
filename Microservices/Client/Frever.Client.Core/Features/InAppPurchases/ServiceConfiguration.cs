using System;
using System.Collections.Generic;
using FluentValidation;
using Frever.Cache.Configuration;
using Frever.Cache.Strategies;
using Frever.Client.Core.Features.InAppPurchases.Contract;
using Frever.Client.Core.Features.InAppPurchases.DataAccess;
using Frever.Client.Core.Features.InAppPurchases.InAppPurchase;
using Frever.Client.Core.Features.InAppPurchases.Offers;
using Frever.Client.Core.Features.InAppPurchases.RefundInAppPurchase;
using Frever.Client.Core.Features.InAppPurchases.Subscriptions;
using Frever.ClientService.Contract.InAppPurchases;
using Frever.Shared.AssetStore;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.Client.Core.Features.InAppPurchases;

public static class ServiceConfiguration
{
    public static void AddInAppPurchasesCore(this IServiceCollection services, InAppPurchaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        services.AddSingleton(options);

        services.AddScoped<IInAppProductRepository, PersistentInAppProductRepository>();
        services.AddScoped<IPurchaseOrderRepository, PersistentPurchaseOrderRepository>();

        services.AddScoped<IValidator<InitInAppPurchaseRequest>, InitInAppPurchaseRequestValidator>();
        services.AddScoped<IValidator<CompleteInAppPurchaseRequest>, CompleteInAppPurchaseRequestValidator>();
        services.AddScoped<IValidator<RefundInAppPurchaseRequest>, RefundInAppPurchaseRequestValidator>();

        services.AddScoped<IStoreTransactionDataValidator, AppStoreApiStoreTransactionDataValidator>();

        services.AddScoped<IPendingOrderManager, PersistentPendingOrderManager>();
        services.AddScoped<IInAppAssetPurchaseManager, PersistentInAppAssetPurchaseManager>();
        services.AddScoped<IInAppPurchaseService, InAppPurchaseService>();
        services.AddScoped<IInAppProductOfferService, InAppProductOfferService>();

        services.AddScoped<IInAppPurchaseRestoreService, InAppPurchaseRestoreService>();

        services.AddScoped<IRefundRepository, PersistentRefundRepository>();
        services.AddScoped<IRefundInAppPurchaseService, RefundInAppPurchaseService>();

        services.AddSingleton<GoogleApiClient>();
        services.AddSingleton<GoogleVoidedPurchaseExtractor>();
        services.AddSingleton<AppleRefundPayloadParser>();

        services.AddAssetStoreTransactions();
        services.AddInAppSubscriptions();

        services.AddScoped<IBalanceService, AssetStoreTransactionBalanceService>();

        services.AddFreverCaching(
            o =>
            {
                o.Redis.Blob<AvailableOffers>(
                    SerializeAs.Json,
                    false,
                    typeof(HardCurrencyExchangeOffer),
                    typeof(InAppProduct),
                    typeof(InAppProductDetails)
                );
                o.InMemory.Blob<List<InAppProductInternal>>(SerializeAs.Protobuf, false, typeof(InAppProduct), typeof(InAppProductDetails));
                o.InMemory.Blob<InAppProductPriceTier[]>(SerializeAs.Protobuf, false, typeof(InAppProductPriceTier));
            }
        );
    }
}