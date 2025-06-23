namespace Frever.AdminService.Core.Services.InAppPurchases.Contracts;

public class InAppProductPriceTierDto
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string AppStoreProductRef { get; set; }
    public string PlayMarketProductRef { get; set; }
    public int RefPriceUsdCents { get; set; }
}