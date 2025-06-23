using System;
using Common.Models.Files;

namespace Frever.ClientService.Contract.Ai;

public class AiGeneratedContentShortInfo : IFileMetadataOwner
{
    public required GroupInfo Group { get; set; }

    public required AiGeneratedContentType Type { get; set; }

    public required long? RemixedFromAiGeneratedContentId { get; set; }

    public required DateTime CreatedAt { get; set; }
    public required long Id { get; set; }
    public required FileMetadata[] Files { get; set; }
}

public class GroupInfo : IFileMetadataOwner
{
    public required string NickName { get; set; }
    public required long Id { get; set; }
    public required FileMetadata[] Files { get; set; }
}

public enum AiGeneratedContentType
{
    Video,
    Image
}

public enum AiGeneratedContentStatus
{
    Draft,
    Published,
    Hidden
}