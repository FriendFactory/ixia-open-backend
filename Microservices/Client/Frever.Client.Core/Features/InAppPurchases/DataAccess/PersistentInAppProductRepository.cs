using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.InAppPurchases.DataAccess;

public class PersistentInAppProductRepository(IWriteDb db) : IInAppProductRepository
{
    public IQueryable<InAppProduct> GetActiveInAppProducts()
    {
        return db.InAppProduct.Where(p => p.IsActive && !p.IsSeasonPass);
    }

    public IQueryable<InAppProductDetails> GetActiveInAppProductDetails(long inAppProductId)
    {
        return db.InAppProductDetails.Where(p => p.InAppProductId == inAppProductId && p.IsActive && p.AssetType == null);
    }

    public IQueryable<InAppProductDetails> GetActiveInAppProductDetailsAll()
    {
        return db.InAppProductDetails.Where(p => p.IsActive)
                 .Where(d => db.InAppProduct.Where(p => p.IsActive).Any(p => p.Id == d.InAppProductId));
    }

    public IQueryable<InAppProductPriceTier> GetPriceTiers()
    {
        return db.InAppProductPriceTier;
    }

    public Task<NestedTransaction> BeginTransaction()
    {
        return db.BeginTransactionSafe();
    }
}