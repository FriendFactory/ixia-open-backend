namespace Frever.Client.Core.Features.Sounds.Song.Models;

public class Genre
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int SortOrder { get; set; }
    public string[] Countries { get; set; }
}