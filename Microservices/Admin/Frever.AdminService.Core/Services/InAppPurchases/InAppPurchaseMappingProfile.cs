using Frever.AdminService.Core.Services.InAppPurchases.Contracts;
using Frever.Shared.MainDb.Entities;
using Profile = AutoMapper.Profile;

namespace Frever.AdminService.Core.Services.InAppPurchases;

// ReSharper disable once UnusedType.Global
public class InAppPurchaseMappingProfile : Profile
{
    public InAppPurchaseMappingProfile()
    {
        CreateMap<InAppProduct, InAppProductDto>();
        CreateMap<InAppPurchaseOrder, InAppPurchaseOrderDto>();
        CreateMap<InAppProduct, InAppProductShortDto>().ReverseMap();
        CreateMap<InAppProductPriceTier, InAppProductPriceTierDto>().ReverseMap();
        CreateMap<InAppProductDetails, InAppProductDetailsDto>().ReverseMap();
        CreateMap<HardCurrencyExchangeOffer, HardCurrencyExchangeOfferDto>().ReverseMap();
    }
}