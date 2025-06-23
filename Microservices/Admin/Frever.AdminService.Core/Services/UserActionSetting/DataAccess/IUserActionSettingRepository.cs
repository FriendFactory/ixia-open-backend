using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Frever.AdminService.Core.Services.UserActionSetting.DataAccess;

public interface IUserActionSettingRepository
{
    IQueryable<Shared.MainDb.Entities.UserActionSetting> GetUserActionSetting();

    Task<bool> CreateOrUpdateAsync(IEnumerable<Shared.MainDb.Entities.UserActionSetting> settings, CancellationToken token = default);

    Task<bool> DeleteAsync(IEnumerable<Shared.MainDb.Entities.UserActionSetting> userActionTypes, CancellationToken token = default);
}