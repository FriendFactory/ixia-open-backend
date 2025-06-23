using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.CommercialMusic;

public interface I7DigitalClient
{
    Task<TrackDetails> GetExternalSongDetails(long externalSongId);
}

public class TrackDetails
{
    [JsonProperty("id")] public long Id { get; set; }

    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("artist")] public ArtistDetails Artist { get; set; }

    [JsonProperty("isrc")] public string Isrc { get; set; }
}

public class ArtistDetails
{
    [JsonProperty("id")] public long Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; }
}