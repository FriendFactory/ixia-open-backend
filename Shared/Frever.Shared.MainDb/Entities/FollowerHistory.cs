using System;
using NpgsqlTypes;

namespace Frever.Shared.MainDb.Entities;

public class FollowerHistory
{
    public long FollowerHistoryId { get; set; }
    public long FollowingId { get; set; }
    public long FollowerId { get; set; }
    public NpgsqlRange<DateTime> TimePeriod { get; set; }

    public virtual Group Follower { get; set; }
    public virtual Group Following { get; set; }
}