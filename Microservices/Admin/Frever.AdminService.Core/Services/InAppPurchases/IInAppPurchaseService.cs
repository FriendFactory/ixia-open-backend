using System.Threading.Tasks;
using Frever.AdminService.Core.Services.InAppPurchases.Contracts;
using Frever.AdminService.Core.Utils;
using Microsoft.AspNet.OData.Query;

namespace Frever.AdminService.Core.Services.InAppPurchases;

public interface IInAppPurchaseService
{
    Task<InAppProductDto> GetInAppProduct(long id);

    Task<ResultWithCount<InAppProductShortDto>> GetInAppProducts(ODataQueryOptions<InAppProductShortDto> options);

    Task<ResultWithCount<InAppProductPriceTierDto>> GetPriceTiers(ODataQueryOptions<InAppProductPriceTierDto> options);

    Task<ResultWithCount<HardCurrencyExchangeOfferDto>> GetHardCurrencyExchangeOffers(
        ODataQueryOptions<HardCurrencyExchangeOfferDto> options
    );

    Task<InAppPurchaseOrderDto[]> GetUserPurchaseHistory(long groupId, int top, int skip);

    Task<InAppProductShortDto> SaveInAppProduct(InAppProductShortDto model);

    Task<InAppProductDetailsDto> SaveInAppProductDetails(InAppProductDetailsDto model);

    Task<InAppProductPriceTierDto> SavePriceTier(InAppProductPriceTierDto model);

    Task<HardCurrencyExchangeOfferDto> SaveHardCurrencyExchangeOffer(HardCurrencyExchangeOfferDto model);

    Task DeleteInAppProduct(long id);

    Task DeletePriceTier(long id);

    Task DeleteHardCurrencyExchangeOffer(long id);
}