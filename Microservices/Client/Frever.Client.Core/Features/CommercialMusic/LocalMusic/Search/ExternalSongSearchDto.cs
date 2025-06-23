namespace Frever.Client.Core.Features.CommercialMusic;

public class ExternalSongSearchDto
{
    public long ExternalTrackId { get; set; }

    public string Isrc { get; set; }

    public string ArtistName { get; set; }

    public string SongName { get; set; }

    public int? SpotifyPopularity { get; set; }
}