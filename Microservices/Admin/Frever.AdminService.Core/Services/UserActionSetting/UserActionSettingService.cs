using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.UserActionSetting.DataAccess;
using Frever.Client.Shared.ActivityRecording;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.UserActionSetting;

public class UserActionSettingService(IUserActionSettingRepository userActionSettingRepository) : IUserActionSettingService
{
    private readonly IUserActionSettingRepository _userActionSettingRepository =
        userActionSettingRepository ?? throw new ArgumentNullException(nameof(userActionSettingRepository));

    public async Task<UserActivitySettings> GetSettingsAsync(CancellationToken token = default)
    {
        var settings = await _userActionSettingRepository.GetUserActionSetting()
                                                         .AsNoTracking()
                                                         .Where(
                                                              e => UserActionSettingHelper.AvailableUserActionTypes.Contains(e.UserAction)
                                                          )
                                                         .ToDictionaryAsync(k => k.UserAction, v => v.Settings, token);

        return settings.Count == 0 ? GetDefaultUserActivitySettings() : UserActionSettingHelper.ConvertToUserActivitySettings(settings);
    }

    public async Task<bool> CreateOrUpdateAsync(UserActivitySettings setting, CancellationToken token = default)
    {
        var settings = UserActionSettingHelper.ConvertToUserActionSetting(setting);

        var dbSettingsToRemove = await _userActionSettingRepository.GetUserActionSetting()
                                                                   .Where(e => !settings.Keys.Contains(e.UserAction))
                                                                   .ToListAsync(token);

        if (dbSettingsToRemove.Count != 0)
            await _userActionSettingRepository.DeleteAsync(dbSettingsToRemove, token);

        return await _userActionSettingRepository.CreateOrUpdateAsync(settings.Values, token);
    }

    private static UserActivitySettings GetDefaultUserActivitySettings()
    {
        return new UserActivitySettings
               {
                   OriginalVideoCreated = new OriginalVideoCreatedConfiguration(),
                   TemplateVideoCreated = new TemplateVideoCreatedConfiguration(),
                   TaskCompletion = new TaskCompletionConfiguration(),
                   VideoLike = new VideoLikeConfiguration(),
                   VideoWatch = new VideoWatchConfiguration(),
                   Login = new LoginConfiguration()
               };
    }
}