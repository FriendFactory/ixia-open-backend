using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Core.Features.ReportInappropriate;

public interface IReportInappropriateVideoService
{
    Task<VideoReportReason[]> GetVideoReportReasons();

    Task<VideoReport> ReportVideo(ReportInappropriateVideoRequest request);
}