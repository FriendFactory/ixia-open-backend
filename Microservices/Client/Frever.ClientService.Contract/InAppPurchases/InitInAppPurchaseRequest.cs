namespace Frever.ClientService.Contract.InAppPurchases;

public class InitInAppPurchaseRequest
{
    /// <summary>
    ///     Use encoded string contains information about assets and other benefits offered to the user.
    /// </summary>
    public string InAppProductOfferKey { get; set; }

    public string ClientCurrency { get; set; }

    public decimal ClientCurrencyPrice { get; set; }
}