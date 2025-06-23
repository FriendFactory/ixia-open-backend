using System.Collections.Generic;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Core.Features.Views.DataAccess;

public interface IRecordVideoViewRepository
{
    Task AppendVideoView(IEnumerable<VideoView> views);
}

public class PersistentRecordVideoViewRepository(IWriteDb db) : IRecordVideoViewRepository
{
    public async Task AppendVideoView(IEnumerable<VideoView> views)
    {
        foreach (var item in views)
            await db.VideoView.AddAsync(item);
        await db.SaveChangesAsync();
    }
}