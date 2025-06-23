using System;
using Frever.ClientService.Contract.Ai;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content;

public class AiGeneratedContentFullInfo
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public AiGeneratedContentType Type { get; set; }
    public AiGeneratedContentStatus Status { get; set; }
    public long? RemixedFromAiGeneratedContentId { get; set; }
    public AiGeneratedImageFullInfo Image { get; set; }
    public AiGeneratedVideoFullInfo Video { get; set; }
    public long? ExternalSongId { get; set; }
    public bool? IsLipSync { get; set; }
    public DateTime CreatedAt { get; set; }
}