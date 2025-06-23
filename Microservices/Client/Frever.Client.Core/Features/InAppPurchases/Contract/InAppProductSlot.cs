namespace Frever.Client.Core.Features.InAppPurchases.Contract;

public class InAppProductSlot
{
    public InAppProductSlotState State { get; set; }

    /// <summary>
    ///     Might be null if slot is empty
    /// </summary>
    public InAppProductOffer Offer { get; set; }
}