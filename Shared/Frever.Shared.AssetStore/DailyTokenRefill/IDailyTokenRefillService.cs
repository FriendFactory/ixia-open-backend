using System.Threading.Tasks;

namespace Frever.Shared.AssetStore.DailyTokenRefill;

public interface IDailyTokenRefillService
{
    /// <summary>
    /// Refill daily tokens for a single group.
    /// </summary>
    Task RefillDailyTokens(long groupId);

    /// <summary>
    /// Refill daily tokens for all active users.
    /// </summary>
    Task BatchRefillDailyTokens(bool forceRefill);
}