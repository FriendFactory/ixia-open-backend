using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Infrastructure;
using FluentValidation;
using Frever.AdminService.Core.Services.InAppPurchases.Contracts;
using Frever.AdminService.Core.Services.InAppPurchases.DataAccess;
using Frever.AdminService.Core.Utils;
using Frever.Shared.AssetStore.OfferKeyCodec;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNet.OData.Query;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.InAppPurchases;

internal sealed class InAppPurchaseService(
    IMapper mapper,
    IInAppPurchaseRepository repo,
    IInAppProductOfferKeyCodec offerCodec,
    IUserPermissionService permissionService,
    IValidator<InAppProductShortDto> productValidator,
    IValidator<InAppProductDetailsDto> productDetailsValidator,
    IValidator<InAppProductPriceTierDto> priceTierValidator,
    IValidator<HardCurrencyExchangeOfferDto> exchangeOfferValidator
) : IInAppPurchaseService
{
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IInAppProductOfferKeyCodec _offerCodec = offerCodec ?? throw new ArgumentNullException(nameof(offerCodec));
    private readonly IInAppPurchaseRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

    private readonly IUserPermissionService _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    private readonly IValidator<HardCurrencyExchangeOfferDto> _exchangeOfferValidator = exchangeOfferValidator ?? throw new ArgumentNullException(nameof(exchangeOfferValidator));
    private readonly IValidator<InAppProductPriceTierDto> _priceTierValidator = priceTierValidator ?? throw new ArgumentNullException(nameof(priceTierValidator));
    private readonly IValidator<InAppProductDetailsDto> _productDetailsValidator = productDetailsValidator ?? throw new ArgumentNullException(nameof(productDetailsValidator));
    private readonly IValidator<InAppProductShortDto> _productValidator = productValidator ?? throw new ArgumentNullException(nameof(productValidator));

    public async Task<InAppProductDto> GetInAppProduct(long id)
    {
        await _permissionService.EnsureHasBankingAccess();

        var product = await _repo.GetAll<InAppProduct>()
                                 .ProjectTo<InAppProductDto>(_mapper.ConfigurationProvider)
                                 .FirstOrDefaultAsync(e => e.Id == id);

        var details = await _repo.GetAll<InAppProductDetails>().Where(e => e.InAppProductId == id).ToArrayAsync();

        product.ProductDetails = _mapper.Map<InAppProductDetailsDto[]>(details);

        return product;
    }

    public async Task<ResultWithCount<InAppProductShortDto>> GetInAppProducts(ODataQueryOptions<InAppProductShortDto> options)
    {
        await _permissionService.EnsureHasBankingAccess();

        return await _repo.GetAll<InAppProduct>()
                          .ProjectTo<InAppProductShortDto>(_mapper.ConfigurationProvider)
                          .ExecuteODataRequestWithCount(options);
    }

    public async Task<ResultWithCount<InAppProductPriceTierDto>> GetPriceTiers(ODataQueryOptions<InAppProductPriceTierDto> options)
    {
        await _permissionService.EnsureHasBankingAccess();

        return await _repo.GetAll<InAppProductPriceTier>()
                          .ProjectTo<InAppProductPriceTierDto>(_mapper.ConfigurationProvider)
                          .ExecuteODataRequestWithCount(options);
    }

    public async Task<ResultWithCount<HardCurrencyExchangeOfferDto>> GetHardCurrencyExchangeOffers(
        ODataQueryOptions<HardCurrencyExchangeOfferDto> options
    )
    {
        await _permissionService.EnsureHasBankingAccess();

        return await _repo.GetAll<HardCurrencyExchangeOffer>()
                          .ProjectTo<HardCurrencyExchangeOfferDto>(_mapper.ConfigurationProvider)
                          .ExecuteODataRequestWithCount(options);
    }

    public async Task<InAppPurchaseOrderDto[]> GetUserPurchaseHistory(long groupId, int top, int skip)
    {
        await _permissionService.EnsureHasBankingAccess();

        var orders = await _repo.GetAll<InAppPurchaseOrder>()
                                .Where(e => e.GroupId == groupId)
                                .OrderByDescending(e => e.CreatedTime)
                                .Skip(skip)
                                .Take(top)
                                .ToArrayAsync();

        var result = new List<InAppPurchaseOrderDto>();
        foreach (var item in orders)
        {
            var order = _mapper.Map<InAppPurchaseOrderDto>(item);
            var payload = await _offerCodec.DecodeUnsafe(item.InAppProductOfferKey);

            order.InAppProductOfferPayload = payload;
            result.Add(order);
        }

        return result.ToArray();
    }

    public async Task<InAppProductShortDto> SaveInAppProduct(InAppProductShortDto model)
    {
        await _permissionService.EnsureHasBankingAccess();

        await _productValidator.ValidateAndThrowAsync(model);

        var product = model.Id == 0
                          ? await _repo.AddBlankItem(new InAppProduct())
                          : await _repo.GetAll<InAppProduct>().FirstOrDefaultAsync(e => e.Id == model.Id);

        if (product == null)
            throw AppErrorWithStatusCodeException.BadRequest($"InAppProduct ID={model.Id} is not found", "InAppProductNotFound");

        _mapper.Map(model, product);

        // TODO: fix files uploading
        // await UploadFiles(product);

        await _repo.SaveChanges();

        return _mapper.Map<InAppProductShortDto>(product);
    }

    public async Task<InAppProductDetailsDto> SaveInAppProductDetails(InAppProductDetailsDto model)
    {
        await _permissionService.EnsureHasBankingAccess();

        await _productDetailsValidator.ValidateAndThrowAsync(model);

        await ValidateDetails(model);

        var productDetails = model.Id == 0
                                 ? await _repo.AddBlankItem(new InAppProductDetails())
                                 : await _repo.GetAll<InAppProductDetails>().FirstOrDefaultAsync(e => e.Id == model.Id);

        if (productDetails == null)
            throw AppErrorWithStatusCodeException.BadRequest(
                $"InAppProductDetails ID={model.Id} is not found",
                "InAppProductDetailsNotFound"
            );

        _mapper.Map(model, productDetails);

        // TODO: fix files uploading
        // await UploadFiles(productDetails);

        await _repo.SaveChanges();

        return _mapper.Map<InAppProductDetailsDto>(productDetails);
    }

    public async Task<InAppProductPriceTierDto> SavePriceTier(InAppProductPriceTierDto model)
    {
        await _permissionService.EnsureHasBankingAccess();

        await _priceTierValidator.ValidateAndThrowAsync(model);

        var priceTier = model.Id == 0
                            ? await _repo.AddBlankItem(new InAppProductPriceTier())
                            : await _repo.GetAll<InAppProductPriceTier>().FirstOrDefaultAsync(e => e.Id == model.Id);

        if (priceTier == null)
            throw AppErrorWithStatusCodeException.BadRequest($"PriceTier ID={model.Id} is not found", "PriceTierNotFound");

        _mapper.Map(model, priceTier);

        await _repo.SaveChanges();

        return _mapper.Map<InAppProductPriceTierDto>(priceTier);
    }

    public async Task<HardCurrencyExchangeOfferDto> SaveHardCurrencyExchangeOffer(HardCurrencyExchangeOfferDto model)
    {
        await _permissionService.EnsureHasBankingAccess();

        await _exchangeOfferValidator.ValidateAndThrowAsync(model);

        var offer = model.Id == 0
                        ? await _repo.AddBlankItem(new HardCurrencyExchangeOffer())
                        : await _repo.GetAll<HardCurrencyExchangeOffer>().FirstOrDefaultAsync(e => e.Id == model.Id);

        if (offer == null)
            throw AppErrorWithStatusCodeException.BadRequest(
                $"HardCurrencyExchangeOffer ID={model.Id} is not found",
                "HardCurrencyExchangeOfferNotFound"
            );

        _mapper.Map(model, offer);

        await _repo.SaveChanges();

        return _mapper.Map<HardCurrencyExchangeOfferDto>(offer);
    }

    public async Task DeleteInAppProduct(long id)
    {
        await _permissionService.EnsureHasBankingAccess();

        await _repo.DeleteInAppProduct(id);
    }

    public async Task DeletePriceTier(long id)
    {
        await _permissionService.EnsureHasBankingAccess();

        await _repo.DeletePriceTier(id);
    }

    public async Task DeleteHardCurrencyExchangeOffer(long id)
    {
        await _permissionService.EnsureHasBankingAccess();

        await _repo.DeleteHardCurrencyExchangeOffer(id);
    }

    private async Task ValidateDetails(InAppProductDetailsDto model)
    {
        IQueryable<InAppProductDetails> GetProduct()
        {
            return _repo.GetAll<InAppProductDetails>().Where(e => e.InAppProductId == model.InAppProductId && e.Id != model.Id);
        }

        if (model.AssetId.HasValue && model.AssetType.HasValue)
        {
            var assetType = (AssetStoreAssetType) model.AssetType;
            var existing = await GetProduct().AnyAsync(e => e.AssetId == model.AssetId && e.AssetType == assetType);
            if (existing)
                throw AppErrorWithStatusCodeException.BadRequest(
                    $"Product already contains details with assetId={model.AssetId} and assetType={model.AssetType.ToString()}",
                    "InAppProductDetailsAssetDuplicate"
                );
        }

        var sameSortOrder = await GetProduct().AnyAsync(e => e.SortOrder == model.SortOrder);
        if (sameSortOrder)
            throw AppErrorWithStatusCodeException.BadRequest(
                $"Product already contains details with SortOrder={model.SortOrder}",
                "InAppProductDetailsSortOrderDuplicate"
            );
    }
}