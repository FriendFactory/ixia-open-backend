namespace Frever.ClientService.Contract.Sounds;

public class ExternalPlaylistFilterModel
{
    public long? Target { get; set; }

    public int TakePrevious { get; set; } = 0;

    public int TakeNext { get; set; } = 20;
}