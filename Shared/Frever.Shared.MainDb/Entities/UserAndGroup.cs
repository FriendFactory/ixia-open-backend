namespace Frever.Shared.MainDb.Entities;

public class UserAndGroup
{
    public long UserId { get; set; }
    public long GroupId { get; set; }

    public virtual Group Group { get; set; }
    public virtual User User { get; set; }
}