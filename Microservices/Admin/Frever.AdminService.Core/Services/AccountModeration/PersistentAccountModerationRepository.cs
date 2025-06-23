using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.DataAccess;
using AuthServer.DataAccess.Entities;
using Common.Infrastructure;
using Common.Infrastructure.Messaging;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Frever.AdminService.Core.Services.AccountModeration;

public class PersistentAccountModerationRepository(AuthServerDbContext authDb, IWriteDb mainDb, ISnsMessagingService snsMessagingService)
    : IAccountModerationRepository
{
    public async Task<(IDbContextTransaction, IDbContextTransaction)> BeginTransaction()
    {
        var mainDbTran = await mainDb.BeginTransaction();
        var authDbTran = await authDb.Database.BeginTransactionAsync();

        return (mainDbTran, authDbTran);
    }

    public Task<IDbContextTransaction> BeginMainDbTransaction()
    {
        return mainDb.BeginTransaction();
    }

    public Task SaveMainDbChanges()
    {
        return mainDb.SaveChangesAsync();
    }

    public Task<User> GetUserByMainGroup(long mainGroupId)
    {
        return mainDb.User.SingleOrDefaultAsync(u => u.MainGroupId == mainGroupId);
    }

    public Task<User> GetUserByIdentityServerId(string identityServerId)
    {
        return mainDb.User.FirstOrDefaultAsync(u => u.IdentityServerId == Guid.Parse(identityServerId));
    }

    public Task<Group> GetGroup(long groupId)
    {
        return mainDb.Group.SingleOrDefaultAsync(g => g.Id == groupId);
    }

    public async Task UpdateUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        await mainDb.SaveChangesAsync();
    }

    public async Task UpdateGroup(Group group)
    {
        ArgumentNullException.ThrowIfNull(group);

        await mainDb.SaveChangesAsync();
    }

    public IQueryable<UserSound> GetDeletedUserSoundsForAccount(long groupId)
    {
        return mainDb.UserSound.Where(e => e.GroupId == groupId);
    }

    public Task<AspNetUser> GetAuthUser(string id)
    {
        return authDb.AspNetUsers.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task UpdateAuthUser(AspNetUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        authDb.AspNetUsers.Update(user);
        await authDb.SaveChangesAsync();
    }

    public async Task DeleteAuthUser(Guid id)
    {
        var idStr = id.ToString();
        var user = await authDb.AspNetUsers.SingleOrDefaultAsync(u => u.Id == idStr);
        if (user != null)
        {
            authDb.AspNetUsers.Remove(user);
            await authDb.SaveChangesAsync();
        }
    }

    public async Task<AspNetUserClaims> GetAuthUserClaim(string userId, string claimType)
    {
        return await authDb.AspNetUserClaims.FirstOrDefaultAsync(c => c.UserId == userId && c.ClaimType == claimType);
    }

    public async Task CreateAuthUserClaim(string userId, string claimType, string claimValue)
    {
        var claim = await authDb.AspNetUserClaims.FirstOrDefaultAsync(
                        c => c.UserId == userId && c.ClaimValue.ToLower() == claimValue.ToLower() && c.ClaimType == claimType
                    );
        if (claim == null)
        {
            var newClaim = new AspNetUserClaims {UserId = userId, ClaimType = claimType, ClaimValue = claimValue};

            authDb.AspNetUserClaims.Add(newClaim);
            await authDb.SaveChangesAsync();
        }
    }

    public async Task UpdateAuthUserClaim(string userId, string claimType, string claimValue)
    {
        var claim = await authDb.AspNetUserClaims.FirstOrDefaultAsync(c => c.UserId == userId && c.ClaimType == claimType);
        if (claim != null)
        {
            claim.ClaimValue = claimValue;

            await authDb.SaveChangesAsync();
        }
    }

    public IQueryable<User> GetUsers()
    {
        return mainDb.User;
    }

    public async Task SetGroupDeletedInMainDb(long groupId, DateTime? deletedAt)
    {
        var group = await mainDb.Group.FindAsync(groupId);

        if (group == null)
            throw AppErrorWithStatusCodeException.BadRequest("Group is not found", "GroupNotFound");

        group.DeletedAt = deletedAt;

        await mainDb.SaveChangesAsync();
        await snsMessagingService.PublishSnsMessageForGroupDeleted(groupId);
    }
}