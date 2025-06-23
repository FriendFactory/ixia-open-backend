using System;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;

namespace Frever.Shared.AssetStore.DataAccess;

public class PersistentAssetStoreTransactionRepository(IWriteDb readDb) : IAssetStoreTransactionRepository
{
    public async Task<long?> FindGroupByEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(email));

        var user = await readDb.User.FirstOrDefaultAsync(u => u.Email == email);

        return user?.MainGroupId;
    }
}