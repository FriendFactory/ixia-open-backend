using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.InAppPurchases.DataAccess;

public interface IInAppProductRepository
{
    Task<NestedTransaction> BeginTransaction();

    IQueryable<InAppProduct> GetActiveInAppProducts();

    IQueryable<InAppProductDetails> GetActiveInAppProductDetails(long inAppProductId);

    IQueryable<InAppProductDetails> GetActiveInAppProductDetailsAll();

    IQueryable<InAppProductPriceTier> GetPriceTiers();
}