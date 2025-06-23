using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.ClientService.Contract.Social;

namespace Frever.Client.Core.Features.Social.Followers;

internal sealed partial class FollowingService
{
    private static readonly Dictionary<string, RecommendationReason> RecommendationReasons =
        new(StringComparer.OrdinalIgnoreCase)
        {
            {"common_friends", RecommendationReason.CommonFriends},
            {"influential", RecommendationReason.Influential},
            {"personalized", RecommendationReason.Personalized},
            {"followback", RecommendationReason.FollowBack}
        };


    public async Task<FollowRecommendation[]> GetPersonalizedFollowRecommendations()
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var headers = _headerAccessor.GetRequestExperimentsHeader();

        var recommendations = await _followRecommendationClient.GetFollowRecommendations(_currentUser, headers);

        return await FromMlRecommendations(recommendations);
    }

    public async Task<FollowRecommendation[]> GetFollowBackRecommendations()
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var headers = _headerAccessor.GetRequestExperimentsHeader();

        var recommendations = await _followRecommendationClient.GetFollowBack(_currentUser, headers);

        return await FromMlRecommendations(recommendations);
    }

    private async Task<FollowRecommendation[]> FromMlRecommendations(MlFollowerRecommendation[] recommendations)
    {
        var groups = recommendations.Select(r => r.GroupId)
                                    .Concat(recommendations.SelectMany(r => r.CommonFriendsGroupIds ?? Enumerable.Empty<long>()))
                                    .ToArray();

        var groupInfo = await _socialSharedService.GetGroupShortInfo(groups);

        return recommendations.Where(e => groupInfo.ContainsKey(e.GroupId))
                              .Select(
                                   r => new FollowRecommendation
                                        {
                                            Group = groupInfo[r.GroupId],
                                            Reason = RecommendationReasons[r.Reason],
                                            CommonFriends = r.CommonFriendsGroupIds.Where(gid => groupInfo.ContainsKey(gid))
                                                             .Select(gid => groupInfo[gid])
                                                             .ToArray()
                                        }
                               )
                              .ToArray();
    }
}