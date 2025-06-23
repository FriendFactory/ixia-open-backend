using System.Threading;
using System.Threading.Tasks;
using Frever.Client.Shared.ActivityRecording;

namespace Frever.AdminService.Core.Services.UserActionSetting;

public interface IUserActionSettingService
{
    Task<UserActivitySettings> GetSettingsAsync(CancellationToken token = default);
    Task<bool> CreateOrUpdateAsync(UserActivitySettings setting, CancellationToken token = default);
}