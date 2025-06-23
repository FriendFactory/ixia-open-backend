using System;
using Common.Infrastructure;
using Common.Infrastructure.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.InAppPurchases.RefundInAppPurchase;

public class AppleRefundPayloadParser
{
    private static readonly string NotificationTypeRefund = "REFUND";

    private readonly ILogger _log;

    public AppleRefundPayloadParser(ILoggerFactory loggerFactory)
    {
        if (loggerFactory == null)
            throw new ArgumentNullException(nameof(loggerFactory));

        _log = loggerFactory.CreateLogger("Frever.InAppPurchase.AppleRefundPayloadParser");
    }

    public ParsedPayload ParsePayload(string signedPayload)
    {
        if (string.IsNullOrWhiteSpace(signedPayload))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(signedPayload));

        _log.LogInformation("Parse token {t}", signedPayload);

        var parts = signedPayload.Split('.');

        if (parts.Length != 3)
            throw AppErrorWithStatusCodeException.BadRequest("Incorrect JWT format", "JwtFormatError");

        var payloadStr = parts[1];

        if (string.IsNullOrWhiteSpace(payloadStr))
            throw AppErrorWithStatusCodeException.BadRequest("Incorrect JWT format", "JwtFormatError");

        var decodedPayload = payloadStr.Base64DecodeSafe();

        _log.LogInformation("Decoded payload: {p}", decodedPayload);

        var payload = JsonConvert.DeserializeObject<DecodedPayloadV2>(decodedPayload);

        if (!StringComparer.OrdinalIgnoreCase.Equals(payload.NotificationType, NotificationTypeRefund))
        {
            _log.LogWarning("Invalid notification type: {nt}", payload.NotificationType);
            return new ParsedPayload {IsRefundRequest = false};
        }

        _log.LogInformation("REFUND NOTIFICATION PARSED");

        var signedTransactionStr = payload.Data.SignedTransactionInfo;
        if (string.IsNullOrWhiteSpace(signedTransactionStr))
            throw AppErrorWithStatusCodeException.BadRequest("Invalid signed transaction info", "InvalidSignedTransactionInfo");

        return new ParsedPayload {OriginalTransactionId = ParseSignedTransactionInfo(signedTransactionStr), IsRefundRequest = true};
    }


    private string ParseSignedTransactionInfo(string signedTransactionInfo)
    {
        if (string.IsNullOrWhiteSpace(signedTransactionInfo))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(signedTransactionInfo));
        _log.LogInformation("Signed Transaction Info: {sti}", signedTransactionInfo);

        var parts = signedTransactionInfo.Split('.');
        if (parts.Length != 3)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid signed transaction info format", "InvalidSignedTransactionInfo");

        var payloadStr = parts[1];
        if (string.IsNullOrWhiteSpace(payloadStr))
            throw AppErrorWithStatusCodeException.BadRequest("Invalid signed transaction info format", "InvalidSignedTransactionInfo");


        var decodedPayload = payloadStr.Base64DecodeSafe();
        ;
        if (string.IsNullOrWhiteSpace(decodedPayload))
            throw AppErrorWithStatusCodeException.BadRequest("Invalid signed transaction info format", "InvalidSignedTransactionInfo");

        var transaction = JsonConvert.DeserializeObject<TransactionDecodedPayload>(decodedPayload);

        if (string.IsNullOrWhiteSpace(transaction.OriginalTransactionId))
            throw AppErrorWithStatusCodeException.BadRequest("Invalid signed transaction info format", "InvalidSignedTransactionInfo");

        return transaction.OriginalTransactionId;
    }
}

public class ParsedPayload
{
    public string OriginalTransactionId { get; set; }

    public bool IsRefundRequest { get; set; }
}

public class DecodedPayloadV2
{
    [JsonProperty("notificationType")] public string NotificationType { get; set; }

    [JsonProperty("subtype")] public string Subtype { get; set; }

    [JsonProperty("data")] public TransactionDataCommon Data { get; set; }
}

public class TransactionDataCommon
{
    [JsonProperty("bundleId")] public string BundleId { get; set; }

    [JsonProperty("signedTransactionInfo")] public string SignedTransactionInfo { get; set; }
}

public class TransactionDecodedPayload
{
    [JsonProperty("bundleId")] public string BundleId { get; set; }

    [JsonProperty("originalTransactionId")] public string OriginalTransactionId { get; set; }
}