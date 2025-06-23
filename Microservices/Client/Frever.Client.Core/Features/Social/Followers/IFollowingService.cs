using System.Threading.Tasks;
using Frever.ClientService.Contract.Social;

namespace Frever.Client.Core.Features.Social.Followers;

public interface IFollowingService
{
    /// <summary>
    ///     Follow to a group
    /// </summary>
    /// <returns></returns>
    Task<Profile> FollowGroupAsync(long groupId);

    /// <summary>
    ///     Stop follow to a group
    /// </summary>
    Task UnFollowGroupAsync(long groupId);

    Task<Profile[]> GetFollowersProfilesAsync(long userMainGroupId, string nickname, int skip, int take);

    Task<Profile[]> GetFollowedProfilesAsync(long userMainGroupId, string nickname, int skip, int take);

    Task<Profile[]> GetFriendProfilesAsync(
        long userMainGroupId,
        string nickname,
        bool canStartChatOnly,
        int skip,
        int take
    );

    Task<FollowRecommendation[]> GetPersonalizedFollowRecommendations();
    Task<FollowRecommendation[]> GetFollowBackRecommendations();
}