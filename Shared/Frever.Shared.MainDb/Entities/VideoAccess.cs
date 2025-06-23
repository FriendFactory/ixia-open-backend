namespace Frever.Shared.MainDb.Entities;

public enum VideoAccess
{
    Public = 0,
    ForFriends = 1,
    ForFollowers = 2,
    Private = 3,
    ForTaggedGroups = 4 // For tagged _FRIENDS_ only
}