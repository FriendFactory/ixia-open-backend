namespace Frever.Client.Core.Features.InAppPurchases.Contract;

public enum InAppProductSlotState
{
    // Offer might be purchased
    Available = 1,

    // Empty Slot without Offer
    Empty = 2,

    // Offer that User already purchased
    SoldOut = 3,

    // Offer which can't be purchased (might be needed in future to advertise Premium pass for example)
    Unavailable = 4
}