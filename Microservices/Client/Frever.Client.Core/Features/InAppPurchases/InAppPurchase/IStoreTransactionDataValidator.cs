using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Frever.Client.Core.Features.AppStoreApi;
using Frever.ClientService.Contract.Common;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.InAppPurchases.InAppPurchase;

public interface IStoreTransactionDataValidator
{
    Task<StoreTransactionValidationResult> ValidateStoreTransaction(ValidateStoreTransactionRequest request);

    Task<SubscriptionValidationResult> ValidateSubscription(Platform platform, string storeOrderIdentifier);
}

public class ValidateStoreTransactionRequest
{
    /// <summary>
    /// Transaction id for Apple and receipt for Google
    /// </summary>
    public string TransactionData { get; set; }

    public bool IsSubscription { get; set; }

    public Platform Platform { get; set; }

    public string AppStoreProductRef { get; set; }

    public string PlayMarketProductRef { get; set; }
}

public class SubscriptionValidationResult
{
    public bool IsActive { get; set; }
}

public class AppStoreApiStoreTransactionDataValidator : IStoreTransactionDataValidator
{
    private static readonly int[] CheckSubscriptionTryoutDelays = [0]; // Tryouts disabled

    private readonly IAppStoreApiClient _appStoreApiClient;
    private readonly GoogleApiClient _googleApiClient;
    private readonly ILogger _log;

    private readonly InAppPurchaseOptions _options;

    public AppStoreApiStoreTransactionDataValidator(
        InAppPurchaseOptions options,
        ILoggerFactory loggerFactory,
        GoogleApiClient googleApiClient,
        IAppStoreApiClient appStoreApiClient
    )
    {
        if (loggerFactory == null)
            throw new ArgumentNullException(nameof(loggerFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _googleApiClient = googleApiClient ?? throw new ArgumentNullException(nameof(googleApiClient));
        _appStoreApiClient = appStoreApiClient ?? throw new ArgumentNullException(nameof(appStoreApiClient));
        _log = loggerFactory.CreateLogger("Frever.InAppPurchase");
    }

    public async Task<StoreTransactionValidationResult> ValidateStoreTransaction(ValidateStoreTransactionRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var requestValidator = new InlineValidator<ValidateStoreTransactionRequest>();
        requestValidator.RuleFor(e => e.AppStoreProductRef).NotEmpty().MinimumLength(1);
        requestValidator.RuleFor(e => e.PlayMarketProductRef).NotEmpty().MinimumLength(1);
        requestValidator.RuleFor(e => e.TransactionData).NotEmpty().MinimumLength(1);

        await requestValidator.ValidateAndThrowAsync(request);

        _log.LogInformation("Validating receipt: Platform{p}, TransactionData={r}", request.Platform, request.TransactionData);

        if (request.Platform == Platform.iOS)
        {
            for (var i = 0; i < CheckSubscriptionTryoutDelays.Length; i++)
            {
                var result = await ValidateAppStoreTransactionData(request);

                if (result.IsValid || i == CheckSubscriptionTryoutDelays.Length - 1)
                    return result;

                await Task.Delay(CheckSubscriptionTryoutDelays[i]);
            }
        }

        if (request.Platform == Platform.Android)
            return await ValidatePlayMarketReceipt(request);

        throw new InvalidOperationException("Unsupported platform");
    }

    public async Task<SubscriptionValidationResult> ValidateSubscription(Platform platform, string storeOrderIdentifier)
    {
        if (platform == Platform.iOS)
        {
            var status = await _appStoreApiClient.CheckSubscriptionStatus(storeOrderIdentifier);
            return new SubscriptionValidationResult {IsActive = status.IsSubscriptionActive};
        }

        throw new NotImplementedException();
    }

    /// <summary>
    /// Validates app store transaction id.
    /// For consumables it's enough to use xxx/transactions/{id} Apple API.
    /// For subscriptions it requires calling xxx/subscriptions/{id} Apple API to correctly detect current active subscription
    /// and compare it with product beign purchased. 
    /// </summary>
    private async Task<StoreTransactionValidationResult> ValidateAppStoreTransactionData(ValidateStoreTransactionRequest request)
    {
        var response = await _appStoreApiClient.CheckAppStoreTransactionStatus(request.TransactionData);

        if (!response.IsValid)
        {
            _log.LogError("Invalid transaction identifier");
            return new StoreTransactionValidationResult {IsValid = false, Error = "Invalid transaction data"};
        }

        if (string.IsNullOrWhiteSpace(response.BundleId) || !response.BundleId.StartsWith(_options.AppStoreBundleIdPrefix))
        {
            _log.LogError("Unknown bundle ID: {bid}", response.BundleId);
            return new StoreTransactionValidationResult
                   {
                       IsValid = false, Error = $"Invalid receipt: unknown bundle ID {response.BundleId}"
                   };
        }
        // For subscription the Apple provides following information (at least in case of downgrading subscription)
        // - transaction data -> product ID you're about to buy (downgraded subscription)
        // - subscription data -> current subscription but with defined expiration date

        // Re-check extended subscription info using extra Apple API
        // Validation returns true if there are any active subscriptions 
        if (response.IsSubscription)
        {
            _log.LogInformation(
                "Validating subscription data: subscription product ID={transactionProductId};  request subscription ID={requestProductId}",
                response.InAppProductId,
                request.AppStoreProductRef
            );

            var subscriptionResponse = await _appStoreApiClient.CheckSubscriptionStatus(request.TransactionData);

            _log.LogInformation(
                "Subscription response data: {subscriptionTransactionData}",
                JsonConvert.SerializeObject(subscriptionResponse)
            );

            if (!response.IsValid)
            {
                _log.LogError("Invalid subscription transaction identifier");
                return new StoreTransactionValidationResult {IsValid = false, Error = "Invalid subscription transaction data"};
            }


            var productSubscriptionData = subscriptionResponse.LastTransactions.Where(s => s.RenewalInfo != null).ToArray();
            if (!productSubscriptionData.Any())
            {
                _log.LogError("No subscription data found {inAppProduct}", request.AppStoreProductRef);
                return new StoreTransactionValidationResult
                       {
                           IsValid = false, Error = "No subscription data found in Apple transaction", IsSubscription = true
                       };
            }

            _log.LogInformation("Subscription data found: {data}", JsonConvert.SerializeObject(productSubscriptionData));

            var activeSubscriptions = productSubscriptionData.Where(p => p.IsActive).ToArray();
            if (!activeSubscriptions.Any())
            {
                _log.LogError("No active subscription found");
                return new StoreTransactionValidationResult
                       {
                           IsValid = true,
                           Error = "No active subscription found",
                           IsSubscription = true,
                           ActiveSubscriptionProductSku = null
                       };
            }

            _log.LogInformation(
                "Active subscription found, activation date {activationDate}, expiration date {renewalDate}",
                activeSubscriptions[0].RenewalInfo?.RecentSubscriptionStartDate,
                activeSubscriptions[0].RenewalInfo?.RenewalDate
            );

            // For subscription we return AppProduct SKU of currently active subscription
            return new StoreTransactionValidationResult
                   {
                       IsValid = true,
                       Environment = response.Environment,
                       AppProductSku = request.AppStoreProductRef,
                       ActiveSubscriptionProductSku = activeSubscriptions[0].RenewalInfo.ProductId,
                       IsSubscription = true,
                       StoreOrderIdentifier = request.TransactionData
                   };
        }
        else
            // Just check transaction info 
        {
            _log.LogInformation(
                "Validating product data: transaction product ID={transactionProductId} is refund={isRefunded}; request product ID={requestProductId}",
                response.InAppProductId,
                response.IsRefunded,
                request.AppStoreProductRef
            );

            var isProductInReceipt = StringComparer.OrdinalIgnoreCase.Equals(response.InAppProductId, request.AppStoreProductRef) &&
                                     !response.IsRefunded;

            if (!isProductInReceipt)
            {
                return new StoreTransactionValidationResult {IsValid = false, Error = "Product not found in transaction data"};
            }

            return new StoreTransactionValidationResult
                   {
                       IsValid = true,
                       AppProductSku = response.InAppProductId,
                       StoreOrderIdentifier = response.TransactionId,
                       Environment = response.Environment,
                   };
        }
    }

    private async Task<StoreTransactionValidationResult> ValidatePlayMarketReceipt(ValidateStoreTransactionRequest request)
    {
        var requestUri = new Uri(
            $"https://androidpublisher.googleapis.com/androidpublisher/v3/applications/{_options.PlayMarketPackageName}/purchases/products/{request.PlayMarketProductRef}/tokens/{request.TransactionData}"
        );

        var (response, status, isSuccessfulStatusCode) = await _googleApiClient.GallGet(requestUri);

        if (!isSuccessfulStatusCode)
        {
            _log.LogError("Error response from {url}: {status} {body}", requestUri, status, response);
            return new StoreTransactionValidationResult
                   {
                       IsValid = false, Error = $"Error validating receipt: HTTP status code {status} || {response}"
                   };
        }

        var inAppInfo = JsonConvert.DeserializeObject<PlayMarketReceiptValidationResponse>(response);
        if (inAppInfo == null)
            throw new InvalidOperationException("Unsupported response format");

        if (inAppInfo.Status != 0)
        {
            _log.LogError("Invalid receipt: status {status}", inAppInfo.Status);
            return new StoreTransactionValidationResult
                   {
                       IsValid = false, Error = $"Invalid receipt: validation status {inAppInfo.Status} || {response}"
                   };
        }

        if (inAppInfo.Kind != PlayMarketReceiptValidationResponse.KindProductPurchase)
        {
            _log.LogError("Invalid receipt kind: {kind}", inAppInfo.Kind);
            return new StoreTransactionValidationResult {IsValid = false, Error = $"Invalid receipt kind {inAppInfo.Kind} || {response}"};
        }

        if (string.IsNullOrWhiteSpace(inAppInfo.OrderId))
        {
            _log.LogError("Unknown OrderId");
            return new StoreTransactionValidationResult {IsValid = false, Error = $"Unknown order ID || {response}"};
        }

        return new StoreTransactionValidationResult
               {
                   IsValid = true,
                   AppProductSku = request.PlayMarketProductRef,
                   StoreOrderIdentifier = inAppInfo.OrderId,
                   Environment = _options.IsProduction ? "production" : "sandbox"
               };
    }
}

public class PlayMarketReceiptValidationResponse
{
    public static readonly string KindProductPurchase = "androidpublisher#productPurchase";

    [JsonProperty("purchaseState")] public int Status { get; set; }

    [JsonProperty("orderId")] public string OrderId { get; set; }

    [JsonProperty("kind")] public string Kind { get; set; }
}

public class StoreTransactionValidationResult
{
    public bool IsValid { get; set; }

    public string AppProductSku { get; set; }

    /// <summary>
    /// Original transaction ID for App Store, can be used to verify subscription status
    /// </summary>
    public string StoreOrderIdentifier { get; set; }

    public string Environment { get; set; }

    public bool IsSubscription { get; set; }

    public string ActiveSubscriptionProductSku { get; set; }

    public string Error { get; set; }
}