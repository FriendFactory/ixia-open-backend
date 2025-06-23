using Frever.Client.Core.Utils.Models;

namespace Frever.Client.Core.Features.Sounds.Playlists;

public class ExternalPlaylistModel : HasReadiness
{
    public long Id { get; set; }
    public string ExternalPlaylistId { get; set; }
    public int SortOrder { get; set; }
    public string[] Countries { get; set; }
}