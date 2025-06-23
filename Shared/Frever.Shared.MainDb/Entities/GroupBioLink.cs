namespace Frever.Shared.MainDb.Entities;

public class GroupBioLink
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public string LinkType { get; set; }
    public string Link { get; set; }
}