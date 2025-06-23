namespace Frever.Video.Contract.AI;

public class TransformationFileUrlResponse
{
    public bool Ok { get; set; }
    public string ErrorMessage { get; set; }
    public string MainFileUrl { get; set; }
    public string CoverFileUrl { get; set; }
    public string ThumbnailFileUrl { get; set; }
    public string MaskFileUrl { get; set; }
    public string Workflow { get; set; }
}