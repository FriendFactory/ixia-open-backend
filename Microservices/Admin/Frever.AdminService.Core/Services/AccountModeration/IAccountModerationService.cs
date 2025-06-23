using System.Threading.Tasks;

namespace Frever.AdminService.Core.Services.AccountModeration;

public interface IAccountModerationService
{
    Task SoftDeleteGroup(long groupId);

    /// <summary>
    ///     WARN: This action is not recoverable.
    /// </summary>
    Task HardDeleteGroup(long groupId);

    Task UpdateUserAuthData(UserAuthData model);
}