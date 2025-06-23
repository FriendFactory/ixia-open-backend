using Frever.Protobuf;

namespace Frever.Client.Shared.CommercialMusic;

public class DeleteSongMessage
{
    public const int MessageVersion = 1;

    [ProtoTopField(1)] public int Version { get; set; } = MessageVersion;

    public long SongId { get; set; }
}