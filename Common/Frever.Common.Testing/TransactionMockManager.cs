using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace Frever.Common.Testing;

public class TransactionMockManager
{
    private readonly List<Mock<IDbContextTransaction>> _runningTransactions = new();

    public async Task<IDbContextTransaction> BeginMockTransaction()
    {
        var result = new Mock<IDbContextTransaction>();
        _runningTransactions.Add(result);
        return result.Object;
    }
}