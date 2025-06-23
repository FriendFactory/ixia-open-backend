namespace Frever.Client.Core.Features.InAppPurchases;

public class CurrencyExchangeOffer
{
    public long Id { get; set; }

    public string Title { get; set; }

    public int HardCurrencyRequired { get; set; }

    public int SoftCurrencyGiven { get; set; }
}