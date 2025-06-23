namespace Frever.Client.Shared.Social;

public class FollowStatCount
{
    public int FriendsCount { get; set; }
    public int FollowingsCount { get; set; }
    public int FollowersCount { get; set; }
}

public class FollowTypeAndCount
{
    public string Type { get; set; }
    public int Count { get; set; }
}