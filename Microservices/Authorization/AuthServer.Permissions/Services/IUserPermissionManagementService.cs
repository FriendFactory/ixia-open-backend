using System.Threading.Tasks;

namespace AuthServer.Permissions.Services;

public interface IUserPermissionManagementService
{
    Task SetGroupBlocked(long groupId, bool isBlocked);

    Task SoftDeleteSelf();

    Task SoftDeleteGroup(long groupId);

    Task UndeleteGroup(long groupId);
}