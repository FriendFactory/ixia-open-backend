using System.Threading.Tasks;

namespace Frever.AdminService.Core.Services.DeviceBlacklist;

public interface IDeviceBlacklistAdminService
{
    Task<DeviceBlacklistDto[]> GetDeviceBlacklist(string search, int skip, int take);
    Task<DeviceBlacklistDto> BlockDevice(BlockDeviceParams request);
    Task UnblockDevice(string deviceId);
}