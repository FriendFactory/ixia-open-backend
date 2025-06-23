using Frever.Client.Core.Features.Sounds.Playlists;
using Frever.Protobuf;
using Frever.Shared.MainDb.Entities;
using Newtonsoft.Json;
using Song = Frever.Client.Core.Features.Sounds.Song.Models.Song;

namespace Frever.Client.Core.Utils.Models;

[ProtobufKnownInheritors(typeof(Song), typeof(ExternalPlaylistModel))]
public abstract class HasReadiness : IStageable
{
    public long ReadinessId { get; set; }

    //TODO: try to remove
    [ProtobufIgnore] [JsonIgnore] public Readiness Readiness { get; set; }
}