using System;
using System.Net;
using System.Threading.Tasks;
using Common.Infrastructure.InternalRequest;
using Common.Infrastructure.Middleware;
using Frever.Client.Core.Features.InAppPurchases;
using Frever.Client.Core.Features.InAppPurchases.Contract;
using Frever.Client.Core.Features.InAppPurchases.InAppPurchase;
using Frever.Client.Core.Features.InAppPurchases.RefundInAppPurchase;
using Frever.ClientService.Contract.Common;
using Frever.ClientService.Contract.InAppPurchases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.InAppPurchases;

[ApiController]
[Authorize]
[Route("/api/in-app-purchase")]
public class InAppPurchaseController(
    IInAppPurchaseService inAppPurchaseService,
    IInAppProductOfferService inAppProductOfferService,
    IRefundInAppPurchaseService refundInAppPurchaseService,
    IInAppPurchaseRestoreService restoreInAppPurchaseService
) : ControllerBase
{
    [HttpGet]
    [Route("offers")]
    [ProducesResponseType((int) HttpStatusCode.OK, Type = typeof(AvailableOffers))]
    public async Task<IActionResult> GetInAppProductOffers()
    {
        var offers = await inAppProductOfferService.GetOffers();
        return Ok(offers);
    }

    /// <summary>
    ///     Makes DB changes according to performed in-app purchase transaction for custom product.
    /// </summary>
    [HttpPost]
    [Route("init")]
    [ProducesResponseType((int) HttpStatusCode.OK, Type = typeof(InitInAppPurchaseResponse))]
    [ProducesResponseType((int) HttpStatusCode.BadRequest, Type = typeof(InitInAppPurchaseResponse))]
    [ProducesResponseType((int) HttpStatusCode.BadRequest, Type = typeof(ErrorDetailsViewModel))]
    public async Task<IActionResult> InitInAppPurchase([FromBody] InitInAppPurchaseRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        var response = await inAppPurchaseService.InitInAppPurchase(request);
        return response.Ok ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    ///     Makes DB changes according to performed in-app purchase transaction for custom product.
    /// </summary>
    [HttpPost]
    [Route("complete")]
    [ProducesResponseType((int) HttpStatusCode.OK, Type = typeof(CompleteInAppPurchaseResponse))]
    [ProducesResponseType((int) HttpStatusCode.BadRequest, Type = typeof(CompleteInAppPurchaseResponse))]
    [ProducesResponseType((int) HttpStatusCode.BadRequest, Type = typeof(ErrorDetailsViewModel))]
    public async Task<IActionResult> CompleteInAppPurchase([FromBody] CompleteInAppPurchaseRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        var response = await inAppPurchaseService.CompleteInAppPurchase(request);
        return response.Ok ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    ///     Makes DB changes according to performed in-app purchase transaction for custom product.
    /// </summary>
    [HttpPost]
    [Route("restore")]
    [ProducesResponseType((int) HttpStatusCode.OK, Type = typeof(CompleteInAppPurchaseResponse))]
    [ProducesResponseType((int) HttpStatusCode.BadRequest, Type = typeof(RestoreInAppPurchaseResult))]
    [ProducesResponseType((int) HttpStatusCode.BadRequest, Type = typeof(ErrorDetailsViewModel))]
    public async Task<IActionResult> RestoreInAppPurchase([FromBody] RestoreInAppPurchaseRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));
        var response = await restoreInAppPurchaseService.RestoreInAppPurchases(request);
        return response.Ok ? Ok(response) : BadRequest(response);
    }


    [HttpPost]
    [InternalEndpoint]
    [Route("test/refund/{platform}/{orderId}")]
    public async Task<IActionResult> TestRefundInAppPurchase([FromRoute] Platform platform, [FromRoute] string orderId)
    {
        await refundInAppPurchaseService.RefundInAppPurchase(
            new RefundInAppPurchaseRequest {Platform = platform, StoreOrderIdentifier = orderId}
        );

        return NoContent();
    }
}