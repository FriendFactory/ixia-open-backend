using System.Linq;
using System.Threading.Tasks;
using AuthServer.Models;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthServer.Repositories;

public interface IUserRepository
{
    Task<IDbContextTransaction> BeginMainDbTransactionAsync();
    Task<IDbContextTransaction> BeginAuthDbTransactionAsync();
    Task SaveChanges();

    Task<MainDbClaimData> GetClaimsDataAsync(string identityServerUserId);
    Task CreateUserAsync(UserCreateModel userData);
    Task UpdateUser(UserUpdateModel userData);
    Task UpdateUser(User user, string countryIso, string languageIso, bool isMinor);

    IQueryable<Group> GetGroupById(long groupId);
    IQueryable<User> GetUserByGroupId(long groupId);
    Task<User> GetUserByAppleId(string appleId);
    Task<User> GetUserByGoogleId(string googleId);
    IQueryable<Group> GetGroupByName(string name);
    IQueryable<Group> GetGroupByIdentityServerId(string identityServerId);
    IQueryable<Country> GetCountries();
    IQueryable<User> AllUsers();
    IQueryable<ApplicationUser> GetAuthDbUsersByUsername(string username);

    Task<bool> IsGroupBlockedForAuthUser(string identityServerId);
    Task<bool> IsEmailRegistered(string email);
    Task<bool> IsPhoneRegistered(string phoneNumber);
    Task<bool> IsNicknameUsed(string nickname);
    Task<bool> IsAppleIdRegistered(string appleId);
    Task<bool> IsGoogleIdRegistered(string googleId);

    Task<bool> AuthUserHasPassword(string identityServerId);
    Task<string> LookupEmailByAppleToken(string appleId);
    Task StoreEmailForAppleId(string appleId, string email);
    Task<bool> IsParentEmailNonUnique(long groupId, string parentEmail);
    Task SetParentEmail(long groupId, string parentEmail, bool isParentAgeValidated);
    Task AddInitialFriend(long currentGroupId, string freverOfficialEmail);
}