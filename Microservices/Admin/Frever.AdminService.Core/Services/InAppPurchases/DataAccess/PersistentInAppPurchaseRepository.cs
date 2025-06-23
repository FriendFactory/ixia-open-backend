using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.InAppPurchases.DataAccess;

public class PersistentInAppPurchaseRepository(IWriteDb db) : IInAppPurchaseRepository
{
    private readonly IWriteDb _db = db ?? throw new ArgumentNullException(nameof(db));

    public IQueryable<T> GetAll<T>()
        where T : class
    {
        return _db.Set<T>();
    }

    public async Task<T> AddBlankItem<T>(T item)
        where T : class
    {
        await _db.Set<T>().AddAsync(item);

        return item;
    }

    public async Task DeleteInAppProduct(long id)
    {
        var product = await _db.InAppProduct.FirstOrDefaultAsync(e => e.Id == id);
        if (product != null)
        {
            _db.InAppProduct.Remove(product);
            await _db.SaveChangesAsync();
        }
    }

    public async Task DeletePriceTier(long id)
    {
        var priceTier = await _db.InAppProductPriceTier.FirstOrDefaultAsync(e => e.Id == id);
        if (priceTier != null)
        {
            _db.InAppProductPriceTier.Remove(priceTier);
            await _db.SaveChangesAsync();
        }
    }

    public async Task DeleteHardCurrencyExchangeOffer(long id)
    {
        var offer = await _db.HardCurrencyExchangeOffer.FirstOrDefaultAsync(e => e.Id == id);
        if (offer != null)
        {
            _db.HardCurrencyExchangeOffer.Remove(offer);
            await _db.SaveChangesAsync();
        }
    }

    public Task<int> SaveChanges()
    {
        return _db.SaveChangesAsync();
    }
}