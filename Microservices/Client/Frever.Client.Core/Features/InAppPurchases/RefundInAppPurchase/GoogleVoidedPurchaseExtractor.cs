using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.InAppPurchases.RefundInAppPurchase;

public class GoogleVoidedPurchaseExtractor
{
    private readonly GoogleApiClient _apiClient;
    private readonly ILogger _log;
    private readonly InAppPurchaseOptions _options;

    public GoogleVoidedPurchaseExtractor(GoogleApiClient apiClient, InAppPurchaseOptions options, ILoggerFactory loggerFactory)
    {
        if (loggerFactory == null)
            throw new ArgumentNullException(nameof(loggerFactory));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _log = loggerFactory.CreateLogger("Frever.GoogleVoidedPurchaseExtractor");
    }

    public async Task<List<VoidedPurchase>> GetVoidedPurchase(DateTime since)
    {
        return await GetVoidedPurchasesPage(since, null);
    }

    private async Task<List<VoidedPurchase>> GetVoidedPurchasesPage(DateTime since, TokenPagination pagination)
    {
        var url =
            $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{_options.PlayMarketPackageName}/purchases/voidedpurchases";

        if (!string.IsNullOrWhiteSpace(pagination?.NextPageToken))
            url += $"&pageSelection.token={pagination.NextPageToken}";

        var uri = new Uri(url, UriKind.Absolute);

        var (body, status, isOk) = await _apiClient.GallGetWithToken(uri);

        _log.LogInformation("Google request: {uri} status={status}", url, status);
        _log.LogInformation("Response: {r}", body);

        if (!isOk)
            throw AppErrorWithStatusCodeException.BadRequest($"Error requesting voided purchase API: {status} {body}", "ApiCallError");

        var result = JsonConvert.DeserializeObject<VoidedPurchaseListResult>(body);

        var voidedPurchases = result.VoidedPurchases ?? new List<VoidedPurchase>();
        if (voidedPurchases.Any() && !string.IsNullOrWhiteSpace(result.TokenPagination?.NextPageToken))
        {
            var nextPage = await GetVoidedPurchasesPage(since, result.TokenPagination);
            voidedPurchases.AddRange(nextPage);
        }

        return voidedPurchases;
    }
}

public class VoidedPurchaseListResult
{
    [JsonProperty("tokenPagination")] public TokenPagination TokenPagination { get; set; }

    [JsonProperty("voidedPurchases")] public List<VoidedPurchase> VoidedPurchases { get; set; }

    [JsonProperty("pageInfo")] public PageInfo PageInfo { get; set; }
}

public class VoidedPurchase
{
    [JsonProperty("orderId")] public string OrderId { get; set; }
}

public class PageInfo
{
    [JsonProperty("totalResults")] public int TotalResults { get; set; }

    [JsonProperty("resultPerPage")] public int ResultsPerPage { get; set; }

    [JsonProperty("startIndex")] public int StartIndex { get; set; }
}

public class TokenPagination
{
    [JsonProperty("nextPageToken")] public string NextPageToken { get; set; }
    [JsonProperty("previousPageToken")] public string PreviousPageToken { get; set; }
}