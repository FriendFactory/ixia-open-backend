using System.Threading.Tasks;
using Frever.ClientService.Contract.Social;

namespace Frever.Client.Core.Features.Social.Profiles;

public interface IProfileService
{
    Task<Profile> GetProfileAsync(long userMainGroupId);

    Task<Profile[]> GetTopProfiles(string nickname, int skip, int count, bool excludeMinors);

    Task<Profile> GetFreverOfficialProfile();

    Task<Profile[]> GetStartFollowRecommendations();

    Task<GroupShortInfo[]> GetGroupsShortInfo(long[] groupIds);
}