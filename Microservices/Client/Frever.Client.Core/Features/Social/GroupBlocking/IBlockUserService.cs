using System.Threading.Tasks;
using Frever.ClientService.Contract.Social;

namespace Frever.Client.Core.Features.Social.GroupBlocking;

public interface IBlockUserService
{
    Task<Profile[]> GetBlockedProfiles();

    Task BlockUser(long blockedGroupId);

    Task UnBlockUser(long blockedGroupId);
}