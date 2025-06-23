using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Frever.ClientService.Contract.Social;

public class FollowRecommendation
{
    public GroupShortInfo Group { get; set; }

    public RecommendationReason Reason { get; set; }

    public GroupShortInfo[] CommonFriends { get; set; }
}

[JsonConverter(typeof(StringEnumConverter))]
public enum RecommendationReason
{
    CommonFriends = 1,
    Influential = 2,
    Personalized = 3,
    FollowBack = 4
}