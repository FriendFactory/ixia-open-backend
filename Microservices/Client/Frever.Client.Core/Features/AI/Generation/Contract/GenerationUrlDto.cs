namespace Frever.Client.Core.Features.AI.Generation.Contract;

public class GenerationUrlDto
{
    public bool Ok { get; set; }
    public string ErrorMessage { get; set; }
    public string MainFileUrl { get; set; }
    public string CoverFileUrl { get; set; }
    public string ThumbnailFileUrl { get; set; }
    public string MaskFileUrl { get; set; }
    public string Workflow { get; set; }
}