using System.Collections.Generic;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Shared.Payouts;

public interface ICurrencyPayoutRepository
{
    Task RecordAssetStoreTransaction(IEnumerable<AssetStoreTransaction> transactions);

    Task<int> GetHardCurrencyBalance(long groupId);

    Task<bool> GroupHasTransaction(long groupId, AssetStoreTransactionType type);
}