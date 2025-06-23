using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.VideoModeration.DataAccess;

public class PersistentReportVideoRepository(IWriteDb db) : IReportVideoRepository
{
    private readonly IWriteDb _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task SaveVideoReport(VideoReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.Id == 0)
            _db.VideoReport.Add(report);
        else
            _db.Entry(report).State = EntityState.Modified;

        await _db.SaveChangesAsync();
    }

    public IQueryable<VideoReport> AllVideoReports()
    {
        return _db.VideoReport;
    }

    public IQueryable<VideoReportReason> GetAllVideoReportReason()
    {
        return _db.VideoReportReason;
    }
}