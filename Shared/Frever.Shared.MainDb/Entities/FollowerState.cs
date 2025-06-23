namespace Frever.Shared.MainDb.Entities;

public enum FollowerState
{
    Pending = 0,
    PendingCloseFriend,
    Following,
    FollowingPendingCloseFriend,
    CloseFriend,
    Blocked,
    Ignored
}