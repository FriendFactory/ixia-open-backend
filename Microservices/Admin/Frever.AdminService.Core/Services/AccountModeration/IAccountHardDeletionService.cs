using System.Threading.Tasks;

namespace Frever.AdminService.Core.Services.AccountModeration;

public interface IAccountHardDeletionService
{
    /// <summary>
    ///     WARN: This action is not recoverable.
    /// </summary>
    Task HardDeleteUserData(long groupId);

    /// <summary>
    ///     WARN: This action is not recoverable.
    /// </summary>
    Task HardDeleteGroups();
}