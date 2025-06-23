using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.DataAccess.Entities;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Frever.AdminService.Core.Services.AccountModeration;

public interface IAccountModerationRepository
{
    Task<(IDbContextTransaction, IDbContextTransaction)> BeginTransaction();
    Task<IDbContextTransaction> BeginMainDbTransaction();
    Task SaveMainDbChanges();

    Task<User> GetUserByMainGroup(long mainGroupId);
    Task<User> GetUserByIdentityServerId(string identityServerId);
    Task<Group> GetGroup(long groupId);
    Task UpdateUser(User user);
    Task UpdateGroup(Group group);
    public IQueryable<User> GetUsers();
    IQueryable<UserSound> GetDeletedUserSoundsForAccount(long groupId);
    Task<AspNetUser> GetAuthUser(string id);
    Task UpdateAuthUser(AspNetUser user);
    Task DeleteAuthUser(Guid id);
    Task<AspNetUserClaims> GetAuthUserClaim(string userId, string claimType);
    Task CreateAuthUserClaim(string userId, string claimType, string claimValue);
    Task UpdateAuthUserClaim(string userId, string claimType, string claimValue);
    Task SetGroupDeletedInMainDb(long groupId, DateTime? deletedAt);
}