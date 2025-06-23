using System.Collections.Generic;
using System.Threading.Tasks;
using Frever.ClientService.Contract.Sounds;

namespace Frever.Client.Core.Features.Sounds.UserSounds.Trending;

public interface ITrendingUserSoundService
{
    Task<List<UserSoundFullInfo>> GetTrendingUserSound(string filter, int skip, int take);
}