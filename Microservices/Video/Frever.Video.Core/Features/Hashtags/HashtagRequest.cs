namespace Frever.Video.Core.Features.Hashtags;

public class HashtagRequest
{
    public int? Skip { get; set; }

    public int Take { get; set; } = 10;
    public string Name { get; set; }
}