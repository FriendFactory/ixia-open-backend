namespace Frever.ClientService.Contract.Sounds;

public class ExternalPlaylistInfo
{
    public long Id { get; set; }
    public string ExternalPlaylistId { get; set; }
    public int SortOrder { get; set; }
}