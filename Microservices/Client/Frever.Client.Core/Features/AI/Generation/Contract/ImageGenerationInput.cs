using System.Collections.Generic;

namespace Frever.Client.Core.Features.AI.Generation.Contract;

public class ImageGenerationInput
{
    public long? AiGeneratedContentId { get; set; }
    public Dictionary<string, string> FileUrls { get; set; }
    public string PromptText { get; set; }
    public long? WardrobeModeId { get; set; }
    public long[] CharacterImageIds { get; set; }
}