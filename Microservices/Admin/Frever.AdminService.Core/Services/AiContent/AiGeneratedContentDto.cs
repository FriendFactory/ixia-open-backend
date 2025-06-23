using System;
using Common.Models.Files;

namespace Frever.AdminService.Core.Services.AiContent;

public class AiGeneratedContentDto
{
    public required long Id { get; set; }
    public required long GroupId { get; set; }
    public required string Type { get; set; }
    public required AiGeneratedVideoDto Video { get; set; }
    public required AiGeneratedImageDto Image { get; set; }
    public required long? ExternalSongId { get; set; }
    public required bool? IsLipSync { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required long? RemixedFromAiGeneratedContentId { get; set; }
}

public class AiGeneratedVideoDto : IFileMetadataOwner
{
    public required long GroupId { get; set; }
    public required string Type { get; set; }
    public required int Length { get; set; }
    public required long? ExternalSongId { get; set; }
    public required bool IsLipSync { get; set; }
    public required string Tts { get; set; }
    public required string Workflow { get; set; }
    public required AiGeneratedVideoClipDto[] Clips { get; set; } = [];
    public required long Id { get; set; }
    public required FileMetadata[] Files { get; set; }
}

public class AiGeneratedVideoClipDto : IFileMetadataOwner
{
    public required string Type { get; set; }
    public required long? AiGeneratedImageId { get; set; }
    public required string Prompt { get; set; }
    public required string ShortPromptSummary { get; set; }
    public required int? Seed { get; set; }
    public required int LengthSec { get; set; }
    public required string Tts { get; set; }
    public required string Workflow { get; set; }

    public required AiGeneratedImageDto Image { get; set; }
    public required long Id { get; set; }
    public required FileMetadata[] Files { get; set; }
}

public class AiGeneratedImageDto : IFileMetadataOwner
{
    public required long GroupId { get; set; }
    public required int NumOfCharacters { get; set; }
    public required int Seed { get; set; }
    public required string Prompt { get; set; }
    public required string ShortPromptSummary { get; set; }
    public required long? AiMakeupId { get; set; }

    public required AiGeneratedImagePersonDto[] Persons { get; set; }
    public required AiGeneratedImageSourceDto[] Sources { get; set; }
    public required long Id { get; set; }
    public required FileMetadata[] Files { get; set; }
}

public class AiGeneratedImagePersonDto : IFileMetadataOwner
{
    public required long? GenderId { get; set; }
    public required long AiGeneratedImageId { get; set; }
    public required long ParticipantGroupId { get; set; }
    public required long ParticipantAiCharacterSelfieId { get; set; }
    public required long Id { get; set; }
    public required FileMetadata[] Files { get; set; }
}

public class AiGeneratedImageSourceDto : IFileMetadataOwner
{
    public required long AiGeneratedImageId { get; set; }
    public required string Type { get; set; }
    public required long Id { get; set; }
    public required FileMetadata[] Files { get; set; }
}