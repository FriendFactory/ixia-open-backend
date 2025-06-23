using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.ReportInappropriate.DataAccess;

public interface IReportInappropriateVideoRepository
{
    IQueryable<VideoReportReason> GetAllVideoReportReason();
    Task<VideoReportReason> GetVideoReportReason(long id);
    Task SaveVideoReport(VideoReport report);
}

public class PersistentReportInappropriateVideoRepository(IWriteDb db) : IReportInappropriateVideoRepository
{
    public IQueryable<VideoReportReason> GetAllVideoReportReason()
    {
        return db.VideoReportReason;
    }

    public async Task<VideoReportReason> GetVideoReportReason(long id)
    {
        return await db.VideoReportReason.SingleOrDefaultAsync(x => x.Id == id);
    }

    public async Task SaveVideoReport(VideoReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.Id == 0)
            db.VideoReport.Add(report);
        else
            db.Entry(report).State = EntityState.Modified;

        await db.SaveChangesAsync();
    }
}