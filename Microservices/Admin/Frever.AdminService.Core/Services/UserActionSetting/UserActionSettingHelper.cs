using System.Collections.Generic;
using Frever.Client.Shared.ActivityRecording;
using Frever.Shared.MainDb.Entities;
using Newtonsoft.Json;

namespace Frever.AdminService.Core.Services.UserActionSetting;

public static class UserActionSettingHelper
{
    public static readonly UserActionType[] AvailableUserActionTypes =
    {
        UserActionType.Login,
        UserActionType.WatchVideo,
        UserActionType.CompleteTask,
        UserActionType.LikeVideo,
        UserActionType.TemplateVideoCreated,
        UserActionType.OriginalVideoCreated
    };

    public static UserActivitySettings ConvertToUserActivitySettings(Dictionary<UserActionType, string> settings)
    {
        return new UserActivitySettings
               {
                   OriginalVideoCreated =
                       JsonConvert.DeserializeObject<OriginalVideoCreatedConfiguration>(
                           settings.GetValueOrDefault(UserActionType.OriginalVideoCreated) ?? string.Empty
                       ),
                   TemplateVideoCreated =
                       JsonConvert.DeserializeObject<TemplateVideoCreatedConfiguration>(
                           settings.GetValueOrDefault(UserActionType.TemplateVideoCreated) ?? string.Empty
                       ),
                   TaskCompletion =
                       JsonConvert.DeserializeObject<TaskCompletionConfiguration>(
                           settings.GetValueOrDefault(UserActionType.CompleteTask) ?? string.Empty
                       ),
                   VideoLike =
                       JsonConvert.DeserializeObject<VideoLikeConfiguration>(
                           settings.GetValueOrDefault(UserActionType.LikeVideo) ?? string.Empty
                       ),
                   VideoWatch =
                       JsonConvert.DeserializeObject<VideoWatchConfiguration>(
                           settings.GetValueOrDefault(UserActionType.WatchVideo) ?? string.Empty
                       ),
                   Login = JsonConvert.DeserializeObject<LoginConfiguration>(
                       settings.GetValueOrDefault(UserActionType.Login) ?? string.Empty
                   )
               };
    }

    public static Dictionary<UserActionType, Shared.MainDb.Entities.UserActionSetting> ConvertToUserActionSetting(
        UserActivitySettings settings
    )
    {
        var properties = typeof(UserActivitySettings).GetProperties();
        var actionTypeProperty = typeof(UserActionConfigurationBase).GetProperty(nameof(UserActionConfigurationBase.ActionType));

        var result = new Dictionary<UserActionType, Shared.MainDb.Entities.UserActionSetting>();

        foreach (var property in properties)
        {
            var value = property.GetValue(settings);

            if (value == null)
                continue;

            var actionType = actionTypeProperty.GetValue(value);

            var setting = new Shared.MainDb.Entities.UserActionSetting
                          {
                              UserAction = (UserActionType) actionType, Settings = JsonConvert.SerializeObject(value)
                          };

            result.Add(setting.UserAction, setting);
        }

        return result;
    }
}