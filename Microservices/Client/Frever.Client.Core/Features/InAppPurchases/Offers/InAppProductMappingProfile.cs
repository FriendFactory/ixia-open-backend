using AutoMapper;
using Frever.Client.Core.Features.InAppPurchases.Contract;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.InAppPurchases.Offers;

public class InAppProductMappingProfile : Profile
{
    public InAppProductMappingProfile()
    {
        CreateMap<InAppProduct, InAppProductOffer>();
        CreateMap<InAppProductDetails, InAppProductOfferDetails>();
        CreateMap<InAppProduct, InAppProductInternal>();
        CreateMap<InAppProductDetails, InAppProductDetailsInternal>();
        CreateMap<InAppProductInternal, InAppProductOffer>();
        CreateMap<InAppProductDetailsInternal, InAppProductOfferDetails>();
    }
}