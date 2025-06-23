namespace Frever.ClientService.Contract.Social;

public class ProfileKpi
{
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public int FriendsCount { get; set; }
    public long VideoLikesCount { get; set; }
    public int PublishedVideoCount { get; set; }
    public int TotalVideoCount { get; set; }
    public int TaggedInVideoCount { get; set; }
    public int MutualFriendsCount { get; set; }
}