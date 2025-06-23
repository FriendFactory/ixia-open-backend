namespace Frever.ClientService.Contract.Sounds;

public class SoundsDto
{
    public SongInfo[] Songs { get; set; }
    public UserSoundFullInfo[] UserSounds { get; set; }
    public ExternalSongDto[] ExternalSongs { get; set; }
}