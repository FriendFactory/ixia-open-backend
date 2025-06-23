using Microsoft.AspNetCore.Http;

namespace Frever.Client.Core.Features.AI.Generation.Contract;

public class PixVerseFileInput
{
    public int Duration { get; set; }
    public string Prompt { get; set; }
    public IFormFile File { get; set; }
}