using System;

namespace Frever.Shared.MainDb.Entities;

public class Follower
{
    public long FollowingId { get; set; }
    public long FollowerId { get; set; }
    public DateTime Time { get; set; }
    public bool IsMutual { get; set; }
    public FollowerState State { get; set; }

    public virtual Group FollowerNavigation { get; set; }
    public virtual Group Following { get; set; }
}