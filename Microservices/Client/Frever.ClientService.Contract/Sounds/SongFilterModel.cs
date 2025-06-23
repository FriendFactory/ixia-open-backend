namespace Frever.ClientService.Contract.Sounds;

public class SongFilterModel
{
    public string Name { get; set; }
    public long? GenreId { get; set; }
    public bool? CommercialOnly { get; set; }
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
    public long[] Ids { get; set; }
}