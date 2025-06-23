using System.Threading.Tasks;

namespace AuthServer.Permissions.Services;

public interface IUserPermissionService
{
    Task<bool> IsAccountActive(long groupId);
    Task EnsureCurrentUserActive();
    Task<bool> IsCurrentUserEmployee();
    Task EnsureCurrentUserEmployee();
    Task EnsureNotCurrentGroup(long groupId);
    Task<bool> IsStarCreator(long groupId);
    Task<bool> IsCurrentUserStarCreator();

    Task<string[]> GetUserReadinessAccessScopes(long groupId);

    Task EnsureHasAssetReadAccess();
    Task EnsureHasAssetFullAccess();
    Task EnsureHasCategoryReadAccess();
    Task EnsureHasCategoryFullAccess();
    Task EnsureHasBankingAccess();
    Task EnsureHasSeasonsAccess();
    Task EnsureHasSettingsAccess();
    Task EnsureHasSocialAccess();
    Task EnsureHasVideoModerationAccess();
    Task EnsureHasChatMessageSendingAccess();
}