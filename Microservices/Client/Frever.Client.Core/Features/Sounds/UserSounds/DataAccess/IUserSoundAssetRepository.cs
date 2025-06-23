using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.Sounds.UserSounds.DataAccess;

public interface IUserSoundAssetRepository
{
    IQueryable<UserSound> GetUserSounds(UserInfo userInfo);
    IQueryable<UserSound> GetUserSoundByIds(long groupId, params long[] ids);
    IQueryable<UserSound> GetTrendingUserSound();
    Task CreateUserSound(UserSound userSound);
    Task RenameUserSound(long id, string newName, long groupId);
    Task<int> SaveChanges();
}