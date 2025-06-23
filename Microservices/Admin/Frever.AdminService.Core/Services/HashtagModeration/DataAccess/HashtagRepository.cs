using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.HashtagModeration.DataAccess;

public class HashtagRepository(IWriteDb db) : IHashtagRepository
{
    private readonly IWriteDb _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<Hashtag> GetByIdAsync(long hashtagId, CancellationToken token = default)
    {
        var hashtag = await _db.Hashtag.SingleOrDefaultAsync(e => !e.IsDeleted && e.Id == hashtagId, token);

        return hashtag;
    }

    public async Task<bool> UpdateAsync(Hashtag hashtag, CancellationToken token = default)
    {
        _db.Hashtag.Update(hashtag);

        return await _db.SaveChangesAsync(token) > 0;
    }

    public IQueryable<Hashtag> GetAll()
    {
        return _db.Hashtag;
    }

    public async Task<bool> SoftDeleteAsync(long hashtagId, CancellationToken token = default)
    {
        var hashtag = await GetByIdAsync(hashtagId, token);

        if (hashtag is null)
            return false;

        hashtag.IsDeleted = true;

        return await _db.SaveChangesAsync(token) > 0;
    }
}