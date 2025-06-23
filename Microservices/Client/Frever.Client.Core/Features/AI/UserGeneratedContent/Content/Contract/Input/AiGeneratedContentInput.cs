namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;

public class AiGeneratedContentInput
{
    public long Id { get; set; }

    public AiGeneratedImageInput Image { get; set; }
    public AiGeneratedVideoInput Video { get; set; }

    public long? RemixedFromAiGeneratedContentId { get; set; }
}