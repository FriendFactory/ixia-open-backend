namespace Frever.Shared.MainDb.Entities;

public class ContentRow
{
    public long Id { get; set; }
    public string Title { get; set; }
    public int SortOrder { get; set; }
    public string TestGroup { get; set; }
    public string ContentType { get; set; }
    public long[] ContentIds { get; set; }
    public string ContentQuery  { get; set; }
    public bool IsEnabled { get; set; }
}