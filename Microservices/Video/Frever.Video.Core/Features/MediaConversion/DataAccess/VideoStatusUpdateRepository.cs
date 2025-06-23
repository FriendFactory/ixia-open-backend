using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Video.Core.Features.MediaConversion.DataAccess;

public interface IVideoStatusUpdateRepository
{
    IQueryable<Frever.Shared.MainDb.Entities.Video> UnsecureGetVideoIncludingDeletedById(long id);
    IQueryable<Frever.Shared.MainDb.Entities.Video> GetRecentNonConvertedVideos(DateTime notBefore);
    Task ClearDeletionMarkFromVideo(long id);
    Task SaveChanges();
}

public class PersistentVideoStatusUpdateRepository(IWriteDb db, ILogger<PersistentVideoStatusUpdateRepository> log)
    : IVideoStatusUpdateRepository
{
    public IQueryable<Frever.Shared.MainDb.Entities.Video> UnsecureGetVideoIncludingDeletedById(long id)
    {
        return db.Video.Include(v => v.VideoMentions).Where(v => v.Id == id);
    }

    public IQueryable<Frever.Shared.MainDb.Entities.Video> GetRecentNonConvertedVideos(DateTime notBefore)
    {
        return db.Video.Where(v => v.ConversionStatus != VideoConversion.Completed)
                 .Where(v => v.TransformedFromVideoId == null ? v.ModifiedTime > notBefore : v.ModifiedTime > notBefore.AddHours(-3));
    }

    public Task SaveChanges()
    {
        return db.SaveChangesAsync();
    }

    public async Task ClearDeletionMarkFromVideo(long id)
    {
        var video = await db.Video.FindAsync(id);
        if (video == null)
            throw new InvalidOperationException($"Video {id} is not found");

        video.IsDeleted = false;
        await db.SaveChangesAsync();
    }
}

public enum LevelVideoConversionStatus
{
    Started = 1,
    Completed = 2,
    Error = 3
}