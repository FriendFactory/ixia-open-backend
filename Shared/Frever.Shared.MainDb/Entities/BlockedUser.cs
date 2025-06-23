namespace Frever.Shared.MainDb.Entities;

public class BlockedUser
{
    public long BlockedUserId { get; set; }
    public long BlockedByUserId { get; set; }
    public virtual Group UserBlocked { get; set; }
    public virtual Group BlockedByUser { get; set; }
}