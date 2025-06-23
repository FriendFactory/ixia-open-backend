using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Jose;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.AppStoreApi.Core;

public partial class HttpAppStoreApiClient
{
    private static readonly AppleEndpoint[] TransactionHistoryEndpoints =
    [
        new AppleEndpoint {Environment = "Sandbox", Url = "https://api.storekit-sandbox.itunes.apple.com/inApps/v2/history"},
        new AppleEndpoint {Environment = "Production", Url = "https://api.storekit.itunes.apple.com/inApps/v2/history"},
    ];

    public async Task<AppStoreTransactionStatus[]> TransactionHistory(string anyTransactionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(anyTransactionId);

        var token = CreateAppStoreJwt();

        using var scope = log.BeginScope("AppStore: Transaction History: TransactionID={transactionId}: ", anyTransactionId);


        foreach (var endpoint in TransactionHistoryEndpoints)
        {
            string revision = String.Empty;

            var jwtTransactions = new List<string>();

            while (true)
            {
                var revisionQuery = String.IsNullOrWhiteSpace(revision) ? String.Empty : $"?revision={revision}";
                var url = $"{endpoint.Url}/{anyTransactionId}{revisionQuery}";

                using var httpRequest = new HttpRequestMessage();
                httpRequest.Method = HttpMethod.Get;
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                httpRequest.RequestUri = new Uri(url);

                using var httpResponse = await httpClient.SendAsync(httpRequest);

                if (!httpResponse.IsSuccessStatusCode)
                    goto nextEndpoint;

                var body = await httpResponse.Content.ReadAsStringAsync();
                var historyResponse = JsonConvert.DeserializeObject<HistoryResponse>(body);
                jwtTransactions.AddRange(historyResponse.SignedTransactions);

                if (historyResponse.HasMore)
                {
                    revision = historyResponse.Revision;
                }
                else
                {
                    var transactions = jwtTransactions.Select(t => JWT.Payload<JwtTransactionInfo>(t))
                                                      .Select(ToAppStoreTransactionData)
                                                      .ToArray();
                    return transactions;
                }
            }

        nextEndpoint:
            { }
        }

        throw new InvalidOperationException("Invalid transaction ID");
    }

    public async Task<SubscriptionStatus> SubscriptionHistory(string anyTransactionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(anyTransactionId);

        return await CheckSubscriptionStatus(anyTransactionId);
    }
}

public class HistoryResponse
{
    [JsonProperty("environment")] public string Environment { get; set; }

    [JsonProperty("bundleId")] public string BundleId { get; set; }

    [JsonProperty("appAppleId")] public long? AppAppleId { get; set; }

    [JsonProperty("hasMore")] public bool HasMore { get; set; }

    [JsonProperty("revision")] public string Revision { get; set; }

    [JsonProperty("signedTransactions")] public List<string> SignedTransactions { get; set; }
}