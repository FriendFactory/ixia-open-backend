using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Frever.Client.Core.Features.InAppPurchases;
using Jose;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.AppStoreApi.Core;

public partial class HttpAppStoreApiClient
{
    private static readonly AppleEndpoint[] TransactionStatusEndpoints =
    [
        new AppleEndpoint {Environment = "Sandbox", Url = "https://api.storekit-sandbox.itunes.apple.com/inApps/v1/transactions"},
        new AppleEndpoint {Environment = "Production", Url = "https://api.storekit.itunes.apple.com/inApps/v1/transactions"},
    ];

    private static readonly AppleEndpoint[] SubscriptionStatusEndpoints =
    [
        new AppleEndpoint {Environment = "Sandbox", Url = "https://api.storekit-sandbox.itunes.apple.com/inApps/v1/subscriptions"},
        new AppleEndpoint {Environment = "Production", Url = "https://api.storekit.itunes.apple.com/inApps/v1/subscriptions"},
    ];

    public async Task<AppStoreTransactionStatus> CheckAppStoreTransactionStatus(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(transactionId));

        var token = CreateAppStoreJwt();

        using var scope = log.BeginScope("AppStore: Check transaction ID={transactionId}: ", transactionId);

        foreach (var endpoint in TransactionStatusEndpoints)
        {
            using var httpRequest = new HttpRequestMessage();
            httpRequest.Method = HttpMethod.Get;
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.RequestUri = new Uri($"{endpoint.Url}/{transactionId}");

            using var httpResponse = await httpClient.SendAsync(httpRequest);
            var body = await httpResponse.Content.ReadAsStringAsync();

            log.LogDebug("Env={env} ResponseStatusCode={status} Body={body}", endpoint.Environment, httpResponse.StatusCode, body);

            if (!httpResponse.IsSuccessStatusCode)
            {
                log.LogDebug("Can't get transaction info via {env} endpoint", endpoint.Environment);
                continue;
            }

            var response = JsonConvert.DeserializeObject<TransactionInfoResponse>(body);
            var payload = JWT.Payload<JwtTransactionInfo>(response.SignedTransactionInfoJwt);

            log.LogInformation("JWT Transaction Payload: {payload}", JsonConvert.SerializeObject(payload));

            return ToAppStoreTransactionData(payload);
        }

        return new AppStoreTransactionStatus {IsValid = false};
    }

    public async Task<SubscriptionStatus> CheckSubscriptionStatus(string transactionId)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(transactionId));

        var token = CreateAppStoreJwt();

        using var scope = log.BeginScope("AppStore: Subscription status: Transaction ID={transactionId}: ", transactionId);

        foreach (var endpoint in SubscriptionStatusEndpoints)
        {
            using var httpRequest = new HttpRequestMessage();
            httpRequest.Method = HttpMethod.Get;
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.RequestUri = new Uri($"{endpoint.Url}/{transactionId}");

            using var httpResponse = await httpClient.SendAsync(httpRequest);
            var body = await httpResponse.Content.ReadAsStringAsync();

            log.LogDebug("Env={env} ResponseStatusCode={status} Body={body}", endpoint.Environment, httpResponse.StatusCode, body);

            if (!httpResponse.IsSuccessStatusCode)
                continue;

            var response = JsonConvert.DeserializeObject<SubscriptionStatusResponse>(body);

            // https://developer.apple.com/documentation/appstoreserverapi/status
            int[] activeSubscriptionStatuses =
            [
                1, // Active
                3, // Billing retry period 
                4  // Billing grace period
            ];

            var transactionData = response.SubscriptionData.SelectMany(d => d.LastTransactions)
                                          .Select(
                                               t =>
                                               {
                                                   var renewal = String.IsNullOrWhiteSpace(t.SignedRenewalInfo)
                                                                     ? null
                                                                     : JWT.Payload<JwtSubscriptionRenewalInfo>(t.SignedRenewalInfo);
                                                   var transaction = JWT.Payload<JwtTransactionInfo>(t.SignedTransactionInfo);

                                                   var data = new SubscriptionTransactionData
                                                              {
                                                                  Status = t.Status,
                                                                  IsActive = activeSubscriptionStatuses.Contains(t.Status),
                                                                  RenewalInfo = renewal == null
                                                                                    ? null
                                                                                    : new SubscriptionRenewalInfo
                                                                                      {
                                                                                          Environment = renewal.Environment,
                                                                                          ProductId = renewal.ProductId,
                                                                                          RenewalDate = DateTimeOffset
                                                                                             .FromUnixTimeMilliseconds(renewal.RenewalDate),
                                                                                          RecentSubscriptionStartDate =
                                                                                              DateTimeOffset.FromUnixTimeMilliseconds(
                                                                                                  renewal.RecentSubscriptionStartDate
                                                                                              ),
                                                                                          OriginalTransactionId =
                                                                                              renewal.OriginalTransactionId,
                                                                                          AutoRenewalProductId = renewal.AutoRenewProductId,
                                                                                          IsInBillingRetryPeriod =
                                                                                              renewal.IsInBillingRetryPeriod
                                                                                      },
                                                                  TransactionInfo = ToAppStoreTransactionData(transaction)
                                                              };

                                                   return data;
                                               }
                                           )
                                          .ToArray();

            var isActive = transactionData.Where(t => t.TransactionInfo.TransactionId == transactionId).Any(t => t.IsActive);
            return new SubscriptionStatus {IsSubscriptionActive = isActive, LastTransactions = transactionData};
        }

        throw new InvalidOperationException($"Error getting information about subscription {transactionId}");
    }

    private static AppStoreTransactionStatus ToAppStoreTransactionData(JwtTransactionInfo payload)
    {
        return new AppStoreTransactionStatus
               {
                   IsValid = true,
                   Environment = payload.Environment,
                   BundleId = payload.BundleId,
                   IsSubscription =
                       payload.Type == JwtTransactionInfo.KnownTypeAutoRenewableSubscription ||
                       payload.Type == JwtTransactionInfo.KnownTypeNonRenewingSubscription,
                   TransactionId = payload.TransactionId,
                   // OriginalTransactionId = payload.OriginalTransactionId,
                   InAppProductId = payload.ProductId,
                   IsRefunded = payload.RevocationDate != 0,
                   TransactionDate = DateTimeOffset.FromUnixTimeMilliseconds(payload.PurchaseDate).DateTime,
                   Currency = payload.Currency,
                   Price = payload.Price / 1000.0M
               };
    }

    private string CreateAppStoreJwt()
    {
        var opt = new InAppPurchaseOptions();
        var certData = Encoding.UTF8.GetString(Convert.FromBase64String(options.KeyDataBase64));
        var key = ECDsa.Create();
        key.ImportFromPem(certData);

        var payload = new Dictionary<string, object>
                      {
                          {"iss", options.IssuerId},
                          {"iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds()},
                          {"exp", DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 30 * 60},
                          {"aud", "appstoreconnect-v1"},
                          {"bid", opt.AppStoreBundleIdPrefix}
                      };

        var token = JWT.Encode(payload, key, JwsAlgorithm.ES256, new Dictionary<string, object> {{"kid", options.KeyId}, {"typ", "JWT"}});
        return token;
    }

    private class AppleEndpoint
    {
        public string Url { get; set; }
        public string Environment { get; set; }
    }
}