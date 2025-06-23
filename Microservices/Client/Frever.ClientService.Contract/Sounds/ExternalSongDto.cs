namespace Frever.ClientService.Contract.Sounds;

public class ExternalSongDto
{
    public long Id { get; set; }
    public bool IsAvailable { get; set; }
    public int UsageCount { get; set; }
    public bool IsFavorite { get; set; }
    public string Key { get; set; }
}