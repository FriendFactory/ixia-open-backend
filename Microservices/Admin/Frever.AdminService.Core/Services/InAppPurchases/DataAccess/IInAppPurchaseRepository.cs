using System.Linq;
using System.Threading.Tasks;

namespace Frever.AdminService.Core.Services.InAppPurchases.DataAccess;

public interface IInAppPurchaseRepository
{
    IQueryable<T> GetAll<T>()
        where T : class;

    Task<T> AddBlankItem<T>(T item)
        where T : class;

    Task DeleteInAppProduct(long id);

    Task DeletePriceTier(long id);

    Task DeleteHardCurrencyExchangeOffer(long id);

    Task<int> SaveChanges();
}