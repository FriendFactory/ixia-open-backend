using System.Linq;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Core.Features.Comments.DataAccess;

public interface IMentionRepository
{
    IQueryable<Group> GetGroupByIds(params long[] groupIds);
}

public class PersistentMentionRepository(IWriteDb db) : IMentionRepository
{
    public IQueryable<Group> GetGroupByIds(params long[] groupIds)
    {
        return db.Group.Where(g => !g.IsBlocked && g.DeletedAt == null).Where(e => groupIds.Contains(e.Id));
    }
}