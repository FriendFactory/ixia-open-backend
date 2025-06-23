using System.Collections.Generic;
using Common.Models.Files;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;

public class AiGeneratedVideoInput : IFileMetadataOwner
{
    public AiGeneratedVideoType Type { get; set; }
    public long? ExternalSongId { get; set; }
    public long? SongId { get; set; }
    public bool IsLipSync { get; set; }
    public string Workflow { get; set; }

    public List<AiGeneratedVideoClipInput> Clips { get; set; } = [];
    public long Id { get; set; }

    public FileMetadata[] Files { get; set; } = [];
}