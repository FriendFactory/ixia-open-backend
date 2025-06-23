using System.Collections.Generic;
using Common.Models.Files;

namespace Frever.Shared.MainDb.Entities;

/// <summary>
///     Video composed of one or more video clips.
///     Files contains final generated video, thumbnails, previews etc.
/// </summary>
public class AiGeneratedVideo : IFileMetadataConfigRoot, IModerationItem
{
    public long GroupId { get; set; }

    public string Type { get; set; } // pan/zoom, image-2-video
    public int LengthSec { get; set; }

    public long? ExternalSongId { get; set; }
    public long? SongId { get; set; }
    public bool IsLipSync { get; set; }
    public string Tts { get; set; }
    public string Workflow { get; set; }

    public bool IsModerationPassed { get; set; }
    public AiContentModerationResult ModerationResult { get; set; }

    public long Id { get; set; }
    public FileMetadata[] Files { get; set; }

    public virtual ICollection<AiGeneratedVideoClip> GeneratedVideoClip { get; set; }
}

/// <summary>
///     A small piece of video used to compose a bigger video.
///     Files contains source video.
/// </summary>
public class AiGeneratedVideoClip : IFileMetadataConfigRoot, IModerationItem
{
    public long AiGeneratedVideoId { get; set; }
    public string Type { get; set; }

    public long? AiGeneratedImageId { get; set; }
    public int Ordinal { get; set; }
    public string Prompt { get; set; }
    public string ShortPromptSummary { get; set; }
    public int? Seed { get; set; }
    public int LengthSec { get; set; }
    public string Tts { get; set; }
    public string Workflow { get; set; }

    public long? UserSoundId { get; set; }

    public bool IsModerationPassed { get; set; }
    public AiContentModerationResult ModerationResult { get; set; }

    public long Id { get; set; }
    public FileMetadata[] Files { get; set; }

    public virtual AiGeneratedImage GeneratedImage { get; set; }
}