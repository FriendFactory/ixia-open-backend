using System.Globalization;
using CsvHelper;
using Frever.Common.IntegrationTesting;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.IntegrationTest.Features.InAppPurchase.Data;

public static class AssetStoreTransactionsData
{
    public static async Task<AssetStoreTransaction> WithAssetStoreTransaction(
        this DataEnvironment dataEnv,
        long groupId,
        AssetStoreTransactionType type,
        int amount,
        DateTime createdAt
    )
    {
        ArgumentNullException.ThrowIfNull(dataEnv);

        var tr = new AssetStoreTransaction
                 {
                     GroupId = groupId,
                     TransactionType = type,
                     TransactionGroup = Guid.NewGuid(),
                     HardCurrencyAmount = amount,
                     CreatedTime = createdAt.ToUniversalTime()
                 };

        dataEnv.Db.AssetStoreTransactions.Add(tr);
        await dataEnv.Db.SaveChangesAsync();

        return tr;
    }

    /// <summary>
    /// Imports asset store transactions as CSV string.
    /// To get the CSV use following SQL:
    ///
    /// select
    ///    "Id",
    ///    "CreatedTime",
    ///    "TransactionType",
    ///    "HardCurrencyAmount"
    /// where xxx
    /// order by "Id";
    /// 
    /// </summary>
    public static async Task<AssetStoreTransaction[]> ImportAssetStoreTransactionCsv(this DataEnvironment dataEnv, long groupId, string csv)
    {
        ArgumentNullException.ThrowIfNull(dataEnv);
        ArgumentException.ThrowIfNullOrWhiteSpace(csv);

        using var reader = new CsvReader(new StringReader(csv), CultureInfo.InvariantCulture);

        var result = new List<AssetStoreTransaction>();

        while (await reader.ReadAsync())
        {
            var dateData = reader.GetField<DateTime>(1);
            var typeData = reader.GetField<AssetStoreTransactionType>(2);
            var amountData = reader.GetField<int>(3);

            var tr = new AssetStoreTransaction
                     {
                         GroupId = groupId,
                         TransactionType = typeData,
                         TransactionGroup = Guid.NewGuid(),
                         HardCurrencyAmount = amountData,
                         CreatedTime = dateData.ToUniversalTime()
                     };

            result.Add(tr);
            dataEnv.Db.AssetStoreTransactions.Add(tr);
        }

        await dataEnv.Db.SaveChangesAsync();

        return result.ToArray();
    }
}