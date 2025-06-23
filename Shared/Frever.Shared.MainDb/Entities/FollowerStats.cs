using System.ComponentModel.DataAnnotations.Schema;

namespace Frever.Shared.MainDb.Entities;

[Table("follower_stats", Schema = "stats")]
public class FollowerStats
{
    [Column("group_id")] public long GroupId { get; set; }

    [Column("following_count")] public int FollowingCount { get; set; }

    [Column("followers_count")] public int FollowersCount { get; set; }

    [Column("friends_count")] public int FriendsCount { get; set; }

    [Column("deleted")] public bool Deleted { get; set; }
}