using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Frever.Shared.MainDb;

namespace Frever.AdminService.Core.Services.UserActionSetting.DataAccess;

public class UserActionSettingRepository(IWriteDb serverDbContext) : IUserActionSettingRepository
{
    private readonly IWriteDb _serverDbContext = serverDbContext ?? throw new ArgumentNullException(nameof(serverDbContext));

    public IQueryable<Shared.MainDb.Entities.UserActionSetting> GetUserActionSetting()
    {
        return _serverDbContext.UserActionSettings;
    }

    public async Task<bool> CreateOrUpdateAsync(
        IEnumerable<Shared.MainDb.Entities.UserActionSetting> settings,
        CancellationToken token = default
    )
    {
        foreach (var setting in settings)
            await CreateOrUpdate(setting);

        return await _serverDbContext.SaveChangesAsync(token) > 0;
    }

    public async Task<bool> DeleteAsync(
        IEnumerable<Shared.MainDb.Entities.UserActionSetting> userActionTypes,
        CancellationToken token = default
    )
    {
        _serverDbContext.UserActionSettings.RemoveRange(userActionTypes);
        return await _serverDbContext.SaveChangesAsync(token) > 0;
    }

    private async Task CreateOrUpdate(Shared.MainDb.Entities.UserActionSetting userActionSetting)
    {
        var setting = await _serverDbContext.UserActionSettings.FindAsync(userActionSetting.UserAction);

        if (setting == null)
            await _serverDbContext.UserActionSettings.AddAsync(userActionSetting);
        else
            setting.Settings = userActionSetting.Settings;
    }
}