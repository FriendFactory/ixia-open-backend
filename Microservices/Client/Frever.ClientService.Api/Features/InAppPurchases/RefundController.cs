using System;
using System.IO;
using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.Client.Core.Features.InAppPurchases.RefundInAppPurchase;
using Frever.ClientService.Contract.Common;
using Frever.ClientService.Contract.InAppPurchases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.ClientService.Api.Features.InAppPurchases;

[ApiController]
[Route("api/in-app-purchase/refund")]
public class RefundController : ControllerBase
{
    private readonly AppleRefundPayloadParser _applePayloadParser;
    private readonly ILogger _log;
    private readonly IRefundInAppPurchaseService _refundService;

    public RefundController(
        ILoggerFactory loggerFactory,
        AppleRefundPayloadParser applePayloadParser,
        IRefundInAppPurchaseService refundService
    )
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _applePayloadParser = applePayloadParser ?? throw new ArgumentNullException(nameof(applePayloadParser));
        _refundService = refundService ?? throw new ArgumentNullException(nameof(refundService));
        _log = loggerFactory.CreateLogger("Frever.InAppPurchase.Refund");
    }

    [HttpPost]
    [Route("apple")]
    [AllowAnonymous]
    public async Task<IActionResult> RefundApple()
    {
        var reader = new StreamReader(Request.Body);

        var body = await reader.ReadToEndAsync();

        _log.LogInformation(
            "Apple Server Notification received: {m} {p}{qs} body {b}",
            Request.Method,
            Request.Path,
            Request.QueryString.ToString(),
            body
        );

        var request = JsonConvert.DeserializeObject<AppleRefundNotificationRequest>(body);

        if (string.IsNullOrEmpty(request.Payload))
            throw AppErrorWithStatusCodeException.BadRequest("Unknown request format", "UnknownRequestFormat");

        var parseResult = _applePayloadParser.ParsePayload(request.Payload);
        if (parseResult.IsRefundRequest)
            await _refundService.RefundInAppPurchase(
                new RefundInAppPurchaseRequest {Platform = Platform.iOS, StoreOrderIdentifier = parseResult.OriginalTransactionId}
            );

        return Ok();
    }
}

public class AppleRefundNotificationRequest
{
    [JsonProperty("signedPayload")] public string Payload { get; set; }
}