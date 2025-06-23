using Microsoft.AspNetCore.Http;

namespace Frever.Video.Contract.AI;

public class PixVerseInput
{
    public int Duration { get; set; }
    public string Prompt { get; set; }
    public IFormFile File { get; set; }
}