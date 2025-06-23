using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.PersonalFeed.DataAccess;

public class EntityFrameworkPersonalFeedRepository(IReadDb db) : IPersonalFeedRepository
{
    private readonly IReadDb _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<IQueryable<VideoView>> GetVideoViews(long groupId)
    {
        var user = await _db.User.FirstOrDefaultAsync(u => u.MainGroupId == groupId);

        return user == null ? null : _db.VideoView.Where(v => v.UserId == user.Id);
    }
}