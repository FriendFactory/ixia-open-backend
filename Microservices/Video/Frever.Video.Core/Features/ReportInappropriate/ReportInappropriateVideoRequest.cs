#pragma warning disable CS8618

namespace Frever.Video.Core.Features.ReportInappropriate;

public class ReportInappropriateVideoRequest
{
    public long VideoId { get; set; }

    public long ReasonId { get; set; }

    public string Message { get; set; }
}