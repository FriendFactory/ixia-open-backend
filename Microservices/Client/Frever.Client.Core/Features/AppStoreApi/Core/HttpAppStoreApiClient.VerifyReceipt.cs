using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.AppStoreApi.Core;

public partial class HttpAppStoreApiClient
{
    private const string AppStoreSandboxReceiptValidationUrl = "https://sandbox.itunes.apple.com/verifyReceipt";
    private const string AppStoreProductionReceiptValidationUrl = "https://buy.itunes.apple.com/verifyReceipt";

    public async Task<AppStoreInAppPurchaseReceipt> VerifyReceipt(string receipt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(receipt);

        var passwordRequest =
            new VerifySubscriptionReceiptRequest {Receipt = receipt, ExcludeOldTransactions = true, SharedSecret = options.SharedSecret};

        var pwdResult = await VerifyReceiptCore(passwordRequest);
        if (pwdResult.Status == 0)
            return pwdResult;

        var request = new VerifyProductReceiptRequest {Receipt = receipt, ExcludeOldTransactions = true};
        return await VerifyReceiptCore(request);
    }

    private async Task<AppStoreInAppPurchaseReceipt> VerifyReceiptCore(VerifyReceiptRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        log.LogInformation("VerifyReceipt request={request}", JsonConvert.SerializeObject(request));

        foreach (var (url, isProduction) in new[]
                                            {
                                                (AppStoreProductionReceiptValidationUrl, true), (AppStoreSandboxReceiptValidationUrl, false)
                                            })
        {
            var body = JsonConvert.SerializeObject(request);

            using var httpRequest =
                new HttpRequestMessage
                {
                    Method = HttpMethod.Post, Content = new StringContent(body), RequestUri = new Uri(url),
                };

            using var client = httpClientFactory.CreateClient();

            client.Timeout = isProduction ? TimeSpan.FromSeconds(30) : TimeSpan.FromSeconds(10);

            try
            {
                using var response = await client.SendAsync(httpRequest);
                var responseBody = await response.Content.ReadAsStringAsync();

                log.LogInformation("VerifyReceipt response: {response}", responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    // First try is production, so on error we try sandbox
                    if (isProduction)
                    {
                        log.LogWarning(
                            "Error response from {url}: {status} {body}. Trying sandbox environment",
                            url,
                            response.StatusCode,
                            responseBody
                        );
                        continue;
                    }

                    log.LogError("Error response from {url}: {status} {body}", url, response.StatusCode, responseBody);
                    throw new InvalidOperationException("Error requesting verifyReceipt endpoint");
                }

                var inAppInfo = JsonConvert.DeserializeObject<AppStoreInAppPurchaseReceipt>(responseBody);
                if (inAppInfo == null)
                    throw new InvalidOperationException("Unsupported response format");

                if (inAppInfo.Status != 0)
                {
                    if (isProduction)
                    {
                        log.LogWarning("Invalid receipt, trying next environment: status {status}", inAppInfo.Status);
                        continue;
                    }

                    log.LogError("Invalid receipt: status {status}", inAppInfo.Status);
                    return inAppInfo;
                }

                if (inAppInfo.Receipt == null)
                    throw new InvalidOperationException("Unsupported response format");

                return inAppInfo;
            }
            catch (TaskCanceledException)
            {
                log.LogError("HttpRequest to URL={url} timed out", url);
            }
        }

        throw new InvalidOperationException("Receipt can't be validated against either production and sandbox environments");
    }
}