using System.Collections.Generic;
using Common.Models.Files;

namespace Frever.Shared.MainDb.Entities;

/// <summary>
///     Metadata for AI generated image
///     Files contains thumbnail and generated image refs
/// </summary>
public class AiGeneratedImage : IFileMetadataConfigRoot, IModerationItem
{
    public long GroupId { get; set; }

    public int NumOfCharacters { get; set; }

    // AI Data
    public int Seed { get; set; }
    public string Prompt { get; set; }
    public string ShortPromptSummary { get; set; }
    public long? AiMakeupId { get; set; }
    public long? AiArtStyleId { get; set; }
    public string Workflow { get; set; }

    public bool IsModerationPassed { get; set; }
    public AiContentModerationResult ModerationResult { get; set; }

    public long Id { get; set; }
    public FileMetadata[] Files { get; set; }

    public virtual ICollection<AiGeneratedImagePerson> GeneratedImagePerson { get; set; }
    public virtual ICollection<AiGeneratedImageSource> GeneratedImageSource { get; set; }
}

/// <summary>
///     Selfies used in the generated image.
///     Might be own user selfie or friends selfies
///     Files contains thumbnails
/// </summary>
public class AiGeneratedImagePerson : IFileMetadataConfigRoot
{
    public long AiGeneratedImageId { get; set; }
    public int Ordinal { get; set; } // Left to right
    public long ParticipantGroupId { get; set; }
    public long ParticipantAiCharacterSelfieId { get; set; }
    public long? GenderId { get; set; }
    public long Id { get; set; }
    public FileMetadata[] Files { get; set; }
}

/// <summary>
///     Images used as additional input for generating an image.
/// </summary>
public class AiGeneratedImageSource : IFileMetadataConfigRoot
{
    public static readonly string KnownTypeOrigin = "Origin";
    public static readonly string KnownTypeMakeUp = "MakeUp";
    public static readonly string KnownTypeOutfit = "Outfit";
    public static readonly string KnownTypeBackground = "Background";
    public static readonly string KnownTypeStyle = "Style";
    public static readonly string KnownTypeCamera = "Camera";
    public static readonly string KnownTypePose = "Pose";
    public static readonly string KnownTypeLighting = "Lighting";
    public long AiGeneratedImageId { get; set; }
    public string Type { get; set; }

    public long Id { get; set; }
    public FileMetadata[] Files { get; set; }
}