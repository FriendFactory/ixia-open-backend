using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Frever.Shared.MainDb.Entities;

/// <summary>
///     Saved metadata for image/video generation.
/// </summary>
public class AiGeneratedContent
{
    public static readonly string KnownTypeImage = "Image";
    public static readonly string KnownTypeVideo = "Video";

    public static readonly string KnownStatusDraft = "Draft";
    public static readonly string KnownStatusPublished = "Published";
    public static readonly string KnownStatusHidden = "Hidden";

    public static readonly string KnownGenerationStatusInProgress = "InProgress";
    public static readonly string KnownGenerationStatusCompleted = "Completed";
    public static readonly string KnownGenerationStatusFailed = "Failed";

    public long Id { get; set; }
    public long GroupId { get; set; }
    public string Type { get; set; }
    public string Status { get; set; } = KnownStatusDraft;
    public long? RemixedFromAiGeneratedContentId { get; set; }
    public long? AiGeneratedImageId { get; set; }
    public long? AiGeneratedVideoId { get; set; }
    public long? ExternalSongId { get; set; }
    public bool? IsLipSync { get; set; }
    public long? DraftAiContentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string GenerationStatus { get; set; } = KnownGenerationStatusInProgress;
    public string GenerationKey { get; set; }
    public AiContentGenerationParameters GenerationParameters { get; set; }

    public virtual AiGeneratedImage GeneratedImage { get; set; }
    public virtual AiGeneratedVideo GeneratedVideo { get; set; }
}

public class AiContentGenerationParameters
{
    public string Type { get; set; }
    public string Workflow { get; set; }
    public string Message { get; set; }
}

public class AiContentModerationResult
{
    /// <summary>
    /// True if all items passed moderation.
    /// </summary>
    public required bool IsPassed { get; set; }

    /// <summary>
    /// Individual item moderation results.
    /// If entity has multiple data to moderate (for example prompt and image) this property will contain results for both
    /// </summary>
    public required AiContentItemModeration[] Items { get; set; } = [];
}

public class AiContentItemModeration
{
    /// <summary>
    /// Gets or sets the type of media moderated.
    /// Possible values are image, text, video
    /// </summary>
    public required string MediaType { get; set; }

    /// <summary>
    /// Identifier of the item being moderated. Like prompt, main-image etc.
    /// </summary>
    public required string ContentId { get; set; }

    /// <summary>
    /// Gets or sets a value to moderate.
    /// Could be path to image if media type is image, or prompt itself if media type is text
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or sets a value indicates if content passed moderation (including applying custom weights).
    /// </summary>
    public required bool IsPassed { get; set; }

    /// <summary>
    /// Gets or sets custom category weights if applicable
    /// </summary>
    // public required Dictionary<string, decimal> CustomCategoryWeights { get; set; } = new();

    /// <summary>
    /// Raw API response
    /// </summary>
    public required OpenAiModerationResponse Response { get; set; }
}

public class OpenAiModerationResponse
{
    /// <summary>
    /// Uniq identifier of moderation request
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }

    /// <summary>
    /// Model used for moderation
    /// </summary>
    [JsonProperty("model")] public string Model { get; set; }

    /// <summary>
    /// An array of moderation result for individual items.
    /// If we run model for text and image simultaneously this property will contain two elements.
    /// </summary>
    [JsonProperty("results")] public OpenAiModerationResult[] Results { get; set; }
}

public class OpenAiModerationResult
{
    [JsonProperty("flagged")] public bool Flagged { get; set; }
    [JsonProperty("categories")] public Dictionary<string, bool> Categories { get; set; }
    [JsonProperty("category_scores")] public Dictionary<string, decimal> CategoryScores { get; set; }
    [JsonProperty("category_applied_input_types")] public Dictionary<string, string[]> CategoryAppliedInputTypes { get; set; }
}

public interface IModerationItem
{
    bool IsModerationPassed { get; set; }

    AiContentModerationResult ModerationResult { get; set; }
}