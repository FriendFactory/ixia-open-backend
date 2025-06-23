using System.Threading.Tasks;
using Frever.ClientService.Contract.Sounds;

namespace Frever.Client.Core.Features.Sounds.UserSounds;

public interface IUserSoundAssetService
{
    Task<UserSoundFullInfo[]> GetUserSoundListAsync(UserSoundFilterModel model);

    Task<UserSoundFullInfo> GetUserSoundById(long id);

    Task<UserSoundFullInfo[]> GetUserSoundByIds(long[] ids);

    Task<UserSoundFullInfo> SaveUserSound(UserSoundCreateModel input);

    Task<UserSoundFullInfo> RenameUserSound(long id, string newName);

    Task<bool> ContainsCopyrightedContent(long id);
}