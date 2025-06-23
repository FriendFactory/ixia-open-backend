using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Cache;
using Frever.Client.Core.Features.InAppPurchases.Contract;
using Frever.Shared.AssetStore.OfferKeyCodec;
using Microsoft.Extensions.Logging;

// ReSharper disable PossibleMultipleEnumeration

namespace Frever.Client.Core.Features.InAppPurchases.Offers;

public partial class InAppProductOfferService
{
    private const int MaxProductSlot = 2;

    public async Task<AvailableOffers> GetOffers()
    {
        using var scope = _logger.BeginScope($"Getting offer, GroupID={_currentUser.UserMainGroupId}: ");

        var offerExpiration = TimeSpan.FromHours(25);

        var offers = await _inAppProductOfferCache.GetOrCache(
                         InAppPurchaseCacheKeys.InAppProductOffers(_currentUser, DateTime.UtcNow),
                         BuildOffers,
                         offerExpiration
                     );

        foreach (var slot in offers.InAppProducts)
        {
            _logger.LogInformation("Slot: State={ss}", slot.State);
            if (slot.Offer != null)
            {
                var payload = await _offerKeyCodec.DecodeAndValidate(_currentUser, slot.Offer.OfferKey);
                _logger.LogInformation(
                    "OfferKey='{key}' ID={id} Details={did} Title={t} AppStoreRef={asr} PlayMarketRef={pmr}",
                    slot.Offer.OfferKey,
                    payload.InAppProductId,
                    string.Join(",", (payload.InAppProductDetailIds ?? Enumerable.Empty<long>()).Select(e => e)),
                    slot.Offer.Title,
                    slot.Offer.AppStoreProductRef,
                    slot.Offer.PlayMarketProductRef
                );

                foreach (var details in slot.Offer.Details)
                    _logger.LogInformation("\t\tHC={hc}", details.HardCurrency);
            }
        }

        return offers;
    }

    private async Task<AvailableOffers> BuildOffers()
    {
        var buyHardCurrencyOffers = await GetHardCurrencyOffers();

        // var inAppProductOffers = await CreateInAppProductOffers();

        return new AvailableOffers
               {
                   //InAppProducts = , // In-app products is not supported yet, only support hard currency and subscription
                   HardCurrencyOffers = buyHardCurrencyOffers.ToArray(), SubscriptionOffers = (await GetSubscriptionOffers()).ToArray()
               };
    }

    private async Task<List<InAppProductOffer>> GetHardCurrencyOffers()
    {
        var products = (await GetInternalProducts()).HardCurrencyOnly().OrderBy(p => p.SortOrder);

        var result = new List<InAppProductOffer>();
        foreach (var item in products)
            result.Add(await ToOffer(item, item.ProductDetails.SelectMany(kvp => kvp.Value)));

        return result;
    }

    private async Task<List<InAppProductOffer>> GetSubscriptionOffers()
    {
        var products = (await GetInternalProducts()).SubscriptionsOnly().OrderBy(p => p.SortOrder);

        var result = new List<InAppProductOffer>();
        foreach (var item in products)
            result.Add(await ToOffer(item, item.ProductDetails.SelectMany(kvp => kvp.Value)));

        return result;
    }

    private async Task<InAppProductSlot[]> CreateInAppProductOffers()
    {
        using var scope = _logger.BeginScope("CreateInAppProductOffers");

        // Get all Products
        var products = (await GetInternalProducts()).ExcludeHardCurrencyOnly().ToArray(); // .ExcludeHardCurrencyOnly();
        _logger.LogDebug("{cnt} in-app products in total", products.Count());

        // Select Products with pub/depub dates
        // Filter out by dates first because it affects final result if user has owning assets
        // If we filter out by assets first the User who have owned today asset
        // will receive un dated offers which is wrong.
        var productActiveToday = products.Where(p => p.PublicationDate != null && p.DepublicationDate != null)
                                         .Where(p => p.PublicationDate <= DateTime.UtcNow && p.DepublicationDate >= DateTime.UtcNow);
        var undatedProducts = products.Where(p => p.PublicationDate == null && p.DepublicationDate == null);

        var hasTodayProduct = productActiveToday.Any();
        var todayProducts = hasTodayProduct ? productActiveToday : undatedProducts;

        var productsWithoutAssets = todayProducts.Select(
                                                      p =>
                                                      {
                                                          var productWithUserAvailableAssets = p.ShallowCopy();
                                                          productWithUserAvailableAssets.ProductDetails = p.ProductDetails
                                                             .Select(
                                                                  kvp => new
                                                                         {
                                                                             UniqueOfferGroup = kvp.Key,
                                                                             Details = kvp.Value.Where(d => d.AssetId == null).ToList()
                                                                         }
                                                              )
                                                             .Where(a => a.Details.Any())
                                                             .ToDictionary(a => a.UniqueOfferGroup, a => a.Details);

                                                          return productWithUserAvailableAssets;
                                                      }
                                                  )
                                                 .ToArray();

        // Select Offer
        var prioritizedProducts = productsWithoutAssets.OrderBy(p => p.SortOrder)
                                                       .ThenBy(p => p.Id)
                                                       .Where(p => !p.IsSeasonPass)
                                                       .Take(MaxProductSlot);


        var slots = prioritizedProducts.Select(p => ToOffer(p, p.ProductDetails.Select(d => d.Value.First())))
                                       .Select(d => new InAppProductSlot {Offer = d.Result, State = InAppProductSlotState.Available})
                                       .ToArray();
        return slots;
    }

    private async Task<InAppProductOffer> ToOffer(InAppProductInternal product, IEnumerable<InAppProductDetailsInternal> details)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(details);

        var result = _mapper.Map<InAppProductInternal, InAppProductOffer>(product);
        result.Details = _mapper.Map<IEnumerable<InAppProductDetailsInternal>, List<InAppProductOfferDetails>>(details);
        result.OfferKey = await _offerKeyCodec.Encode(
                              _currentUser,
                              new InAppProductOfferPayload
                              {
                                  InAppProductId = product.Id, InAppProductDetailIds = details.Select(d => d.Id).ToArray()
                              }
                          );
        return result;
    }
}