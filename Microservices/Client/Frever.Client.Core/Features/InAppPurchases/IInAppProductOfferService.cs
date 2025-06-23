using System.Threading.Tasks;
using Frever.Client.Core.Features.InAppPurchases.Contract;

namespace Frever.Client.Core.Features.InAppPurchases;

public interface IInAppProductOfferService
{
    Task<AvailableOffers> GetOffers();

    Task<InAppProductOffer> GetInAppProductOfferLimited(string offerKey);

    Task MarkOfferAsPurchased(string offerKey);
}