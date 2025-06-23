using System.Collections.Generic;

namespace Frever.Client.Core.Features.CommercialMusic;

public class LicensedTrack
{
    public long ExternalTrackId { get; set; }

    public string Isrc { get; set; }

    public string Artist { get; set; }

    public string Title { get; set; }

    public int SpotifyPopularity { get; set; }

    public HashSet<string> AllowedCountries { get; set; }
}