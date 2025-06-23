using System.Threading.Tasks;

namespace Frever.Shared.AssetStore.DataAccess;

public interface IAssetStoreTransactionRepository
{
    Task<long?> FindGroupByEmail(string email);
}