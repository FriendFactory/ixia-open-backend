using System;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.InAppPurchases;
using Frever.AdminService.Core.Services.InAppPurchases.Contracts;
using Frever.AdminService.Core.Services.InAppPurchases.OfferGenerationInfo;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/in-app-purchase")]
public class InAppPurchaseController(
    IInAppOfferGenerationInfoService appOfferGenerationInfoService,
    IInAppPurchaseService appPurchaseService
) : ControllerBase
{
    private readonly IInAppOfferGenerationInfoService _appOfferGenerationInfoService = appOfferGenerationInfoService ?? throw new ArgumentNullException(nameof(appOfferGenerationInfoService));
    private readonly IInAppPurchaseService _appPurchaseService = appPurchaseService ?? throw new ArgumentNullException(nameof(appPurchaseService));

    [HttpGet]
    [Route("offers/generation-info/{groupId}")]
    public async Task<IActionResult> GetInAppPurchaseGenerationInfo([FromRoute] long groupId)
    {
        var data = await _appOfferGenerationInfoService.GetOfferGenerationDebugInfo(groupId);
        if (data == null)
            return NoContent();
        return Ok(data);
    }

    [HttpGet]
    [Route("products")]
    public async Task<IActionResult> GetInAppProducts(ODataQueryOptions<InAppProductShortDto> options)
    {
        var result = await _appPurchaseService.GetInAppProducts(options);

        return Ok(result);
    }

    [HttpGet]
    [Route("product/{id}")]
    public async Task<IActionResult> GetInAppProduct(long id)
    {
        var result = await _appPurchaseService.GetInAppProduct(id);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpGet]
    [Route("price-tiers")]
    public async Task<IActionResult> GetPriceTiers(ODataQueryOptions<InAppProductPriceTierDto> options)
    {
        var result = await _appPurchaseService.GetPriceTiers(options);

        return Ok(result);
    }

    [HttpGet]
    [Route("exchange-offers")]
    public async Task<IActionResult> GetPriceTiers(ODataQueryOptions<HardCurrencyExchangeOfferDto> options)
    {
        var result = await _appPurchaseService.GetHardCurrencyExchangeOffers(options);

        return Ok(result);
    }

    [HttpGet]
    [Route("purchase-history/{groupId}")]
    public async Task<IActionResult> GetUserPurchaseHistory(
        [FromRoute] long groupId,
        [FromQuery(Name = "$top")] int top = 100,
        [FromQuery(Name = "$skip")] int skip = 0
    )
    {
        var result = await _appPurchaseService.GetUserPurchaseHistory(groupId, top, skip);

        return Ok(result);
    }

    [HttpPost]
    [Route("product")]
    public async Task<IActionResult> SaveInAppProduct(InAppProductShortDto model)
    {
        var result = await _appPurchaseService.SaveInAppProduct(model);

        return Ok(result);
    }

    [HttpPost]
    [Route("product-details")]
    public async Task<IActionResult> SaveInAppProductDetails(InAppProductDetailsDto model)
    {
        var result = await _appPurchaseService.SaveInAppProductDetails(model);

        return Ok(result);
    }

    [HttpPost]
    [Route("price-tier")]
    public async Task<IActionResult> SavePriceTier(InAppProductPriceTierDto model)
    {
        var result = await _appPurchaseService.SavePriceTier(model);

        return Ok(result);
    }

    [HttpPost]
    [Route("exchange-offer")]
    public async Task<IActionResult> SaveHardCurrencyExchangeOffer(HardCurrencyExchangeOfferDto model)
    {
        var result = await _appPurchaseService.SaveHardCurrencyExchangeOffer(model);

        return Ok(result);
    }

    [HttpDelete]
    [Route("product/{id}")]
    public async Task<IActionResult> DeleteInAppProduct(long id)
    {
        await _appPurchaseService.DeleteInAppProduct(id);

        return NoContent();
    }

    [HttpDelete]
    [Route("price-tier/{id}")]
    public async Task<IActionResult> DeletePriceTier(long id)
    {
        await _appPurchaseService.DeletePriceTier(id);

        return NoContent();
    }

    [HttpDelete]
    [Route("exchange-offer/{id}")]
    public async Task<IActionResult> DeleteHardCurrencyExchangeOffer(long id)
    {
        await _appPurchaseService.DeleteHardCurrencyExchangeOffer(id);

        return NoContent();
    }
}