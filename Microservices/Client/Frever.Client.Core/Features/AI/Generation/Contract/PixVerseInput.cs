namespace Frever.Client.Core.Features.AI.Generation.Contract;

public class PixVerseInput
{
    public long AiGeneratedContentId { get; set; }
    public int Duration { get; set; }
    public string Prompt { get; set; }
}