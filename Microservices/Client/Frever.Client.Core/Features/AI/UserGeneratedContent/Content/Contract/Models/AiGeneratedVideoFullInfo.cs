using System.Collections.Generic;
using Common.Models.Files;
using Frever.Client.Shared.Files;
using Frever.Protobuf;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content;

public class AiGeneratedVideoFullInfo : IFileMetadataOwner
{
    public long GroupId { get; set; }

    public AiGeneratedVideoType Type { get; set; }
    public int LengthSec { get; set; }

    public long? ExternalSongId { get; set; }
    public bool IsLipSync { get; set; }
    public string Tts { get; set; }
    public string Workflow { get; set; }
    public List<AiGeneratedVideoClipFullInfo> Clips { get; set; }
    public long Id { get; set; }

    public FileMetadata[] Files { get; set; } = [];
    [ProtoNewField(1)] public long? SongId { get; set; }
}

public enum AiGeneratedVideoType
{
    Pan,
    Zoom,
    ImageToVideo
}

public class AiGeneratedVideoFileConfig : DefaultFileMetadataConfiguration<Frever.Shared.MainDb.Entities.AiGeneratedVideo>
{
    public AiGeneratedVideoFileConfig()
    {
        AddMainFile("mp4");
    }
}