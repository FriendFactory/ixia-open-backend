namespace Frever.Client.Core.Features.AI.Generation.Contract;

public class GenerationResult(long id)
{
    public long AiContentId { get; set; } = id;
}