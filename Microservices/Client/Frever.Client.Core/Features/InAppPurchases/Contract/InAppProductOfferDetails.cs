using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.InAppPurchases.Contract;

public class InAppProductOfferDetails
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int? HardCurrency { get; set; }
    public UsageEstimation UsageEstimation { get; set; }
}