using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.VideoModeration.DataAccess;

public interface IReportVideoRepository
{
    Task SaveVideoReport(VideoReport report);

    IQueryable<VideoReport> AllVideoReports();

    IQueryable<VideoReportReason> GetAllVideoReportReason();
}