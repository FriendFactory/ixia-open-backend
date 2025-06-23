using System.Collections.Generic;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Characters.Contracts;

public class AiCharacterImageGenerationInput
{
    public int Age { get; set; }
    public long GenderId { get; set; }
    public long ArtStyleId { get; set; }
    public string Ethnicity { get; set; }
    public string HairStyle { get; set; }
    public string HairColor { get; set; }
    public Dictionary<string, string> FileUrls { get; set; }
}