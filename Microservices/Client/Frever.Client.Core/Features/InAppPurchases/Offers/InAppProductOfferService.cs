using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using AutoMapper;
using Common.Infrastructure;
using Frever.Cache;
using Frever.Client.Core.Features.InAppPurchases.Contract;
using Frever.Client.Core.Features.InAppPurchases.DataAccess;
using Frever.Shared.AssetStore.OfferKeyCodec;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Core.Features.InAppPurchases.Offers;

public partial class InAppProductOfferService : IInAppProductOfferService
{
    private readonly UserInfo _currentUser;
    private readonly IBlobCache<List<InAppProductInternal>> _inAppProductCache;
    private readonly IBlobCache<AvailableOffers> _inAppProductOfferCache;
    private readonly ILogger _logger;
    private readonly IMapper _mapper;
    private readonly IInAppProductOfferKeyCodec _offerKeyCodec;
    private readonly IInAppProductRepository _productRepository;

    public InAppProductOfferService(
        IInAppProductRepository productRepository,
        IBlobCache<AvailableOffers> inAppProductOfferCache,
        IInAppProductOfferKeyCodec offerKeyCodec,
        IMapper mapper,
        IBlobCache<List<InAppProductInternal>> inAppProductCache,
        UserInfo currentUser,
        ILoggerFactory loggerFactory
    )
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _inAppProductOfferCache = inAppProductOfferCache ?? throw new ArgumentNullException(nameof(inAppProductOfferCache));
        _offerKeyCodec = offerKeyCodec ?? throw new ArgumentNullException(nameof(offerKeyCodec));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _inAppProductCache = inAppProductCache;
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));

        _logger = loggerFactory.CreateLogger("Frever.OfferGeneration");
    }

    public async Task<InAppProductOffer> GetInAppProductOfferLimited(string offerKey)
    {
        if (string.IsNullOrWhiteSpace(offerKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(offerKey));

        var offer = await _offerKeyCodec.DecodeAndValidate(_currentUser, offerKey);

        var product = await _productRepository.GetActiveInAppProducts().SingleOrDefaultAsync(p => p.Id == offer.InAppProductId);
        if (product == null)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid in-app product", "InvalidInAppProduct");

        var details = await _productRepository.GetActiveInAppProductDetails(product.Id)
                                              .Where(d => offer.InAppProductDetailIds.Contains(d.Id))
                                              .ToListAsync();

        if (details.Count != (offer.InAppProductDetailIds?.Length ?? 0))
            throw AppErrorWithStatusCodeException.BadRequest(
                "Some items from the offer is not available, please choose another product.",
                "UnavailableOfferItems"
            );

        var result = _mapper.Map<InAppProduct, InAppProductOffer>(product);
        result.Details = _mapper.Map<List<InAppProductDetails>, List<InAppProductOfferDetails>>(details);
        result.OfferKey = offerKey;

        return result;
    }

    public async Task MarkOfferAsPurchased(string offerKey)
    {
        await _inAppProductOfferCache.TryModifyInPlace(
            InAppPurchaseCacheKeys.InAppProductOffers(_currentUser, DateTime.UtcNow),
            offers =>
            {
                var slot = offers.InAppProducts?.FirstOrDefault(s => s.Offer?.OfferKey == offerKey);
                if (slot != null)
                    slot.State = InAppProductSlotState.SoldOut;

                return Task.FromResult(offers);
            }
        );
    }

    private async Task<List<InAppProductInternal>> GetInternalProducts()
    {
        return await _inAppProductCache.GetOrCache(
                   InAppPurchaseCacheKeys.InAppProductInternal,
                   ReadInAppProductInternalFromDb,
                   TimeSpan.FromDays(7)
               );
    }

    private async Task<List<InAppProductInternal>> ReadInAppProductInternalFromDb()
    {
        var products = await _productRepository.GetActiveInAppProducts().ToArrayAsync();
        var details = await _productRepository.GetActiveInAppProductDetailsAll().ToArrayAsync();

        var result = new List<InAppProductInternal>();

        foreach (var p in products)
        {
            var internalProduct = _mapper.Map<InAppProduct, InAppProductInternal>(p);

            var detailsInternal = details.Where(d => d.InAppProductId == p.Id)
                                         .GroupBy(
                                              d => d.UniqueOfferGroup,
                                              d => _mapper.Map<InAppProductDetails, InAppProductDetailsInternal>(d)
                                          )
                                         .ToArray();

            internalProduct.ProductDetails = new Dictionary<int, List<InAppProductDetailsInternal>>();

            foreach (var g in detailsInternal)
                internalProduct.ProductDetails[g.Key] = g.ToList();

            var maxGem = internalProduct.ProductDetails.SelectMany(a => a.Value)
                                        .Where(a => a.HardCurrency != null)
                                        .MaxBy(a => a.HardCurrency);
            internalProduct.GemCount = maxGem?.HardCurrency;

            result.Add(internalProduct);
        }

        return result;
    }
}