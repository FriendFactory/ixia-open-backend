using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using Common.Infrastructure;
using Frever.Client.Core.Utils;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.Sounds.UserSounds.DataAccess;

internal sealed class UserSoundAssetRepository(IWriteDb db) : IUserSoundAssetRepository
{
    public IQueryable<UserSound> GetUserSounds(UserInfo userInfo)
    {
        return db.UserSound.Where(us => us.DeletedAt == null).AccessibleForUser(userInfo);
    }

    public IQueryable<UserSound> GetUserSoundByIds(long groupId, params long[] ids)
    {
        return db.UserSound.Where(us => ids.Contains(us.Id)).Where(us => us.DeletedAt == null).Where(us => us.GroupId == groupId);
    }

    public async Task CreateUserSound(UserSound userSound)
    {
        ArgumentNullException.ThrowIfNull(userSound);

        if (userSound.Name != null)
            userSound.Name = userSound.Name.Trim();

        await using var transaction = await db.BeginTransactionSafe();

        var grp = await db.Group.FindAsync(userSound.GroupId);
        var totalUserSounds = await db.UserSound.CountAsync(us => us.GroupId == userSound.GroupId);

        if (string.IsNullOrWhiteSpace(userSound.Name))
            userSound.Name = $"{grp.NickName} {totalUserSounds + 1}";
        else
            await EnsureUserSoundNameUnique(0, userSound.Name);

        await db.UserSound.AddAsync(userSound);
        await db.SaveChangesAsync();
        await transaction.Commit();
    }

    public Task<int> SaveChanges()
    {
        return db.SaveChangesAsync();
    }

    public IQueryable<UserSound> GetTrendingUserSound()
    {
        return db.UserSound.Where(us => us.DeletedAt == null && us.Group.DeletedAt == null && !us.Group.IsBlocked)
                 .OrderByDescending(s => s.UsageCount)
                 .AsNoTracking();
    }

    public async Task RenameUserSound(long id, string newName, long groupId)
    {
        await using var transaction = await db.BeginTransactionSafe();

        await EnsureUserSoundNameUnique(id, newName);

        var existing = await db.UserSound.FindAsync(id);
        if (existing == null)
            throw AppErrorWithStatusCodeException.NotFound("User sound is not found", "UserSoundNotFound");
        if (existing.GroupId != groupId)
            throw AppErrorWithStatusCodeException.NotAuthorized("User sound cannot be changed", "UserSoundFromOtherUser");

        existing.Name = newName;
        await db.SaveChangesAsync();
        await transaction.Commit();
    }

    private async Task EnsureUserSoundNameUnique(long id, string name)
    {
        var nonUnique = await db.UserSound.AnyAsync(us => us.Id != id && us.Name == name);
        if (nonUnique)
            throw AppErrorWithStatusCodeException.BadRequest("Name is not unique", "UserSoundNameNotUnique");
    }
}