using System.Threading.Tasks;

namespace Frever.AdminService.Core.Services.RoleModeration;

public interface IRoleModerationService
{
    Task<AccessScopeDto[]> GetAccessScopes();
    Task<AccessScopeDto[]> GetUserAccessScopes(long groupId);
    Task<RoleDto[]> GetRoles(int skip, int take);
    Task<UserRoleDto[]> GetUserRoles(string email, long? roleId, int skip, int take);
    Task SaveRole(RoleModel model);
    Task SaveUserRole(UserRoleModel model);
    Task DeleteRole(long id);
}