using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Data;
using AuthServer.Models;
using AuthServer.Permissions.Services;
using Common.Infrastructure;
using Common.Infrastructure.Messaging;
using Common.Models;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AuthServer.Repositories;

internal class UserRepository(
    IWriteDb writeDb,
    ApplicationDbContext authDb,
    ILoggerFactory loggerFactory,
    ISnsMessagingService snsMessagingService
) : IUserRepository
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("Frever.Auth.UserRepository");

    public async Task CreateUserAsync(UserCreateModel userData)
    {
        var country = await GetCountryByIsoCode(userData.Country);

        _logger.LogInformation("Country provided {}; country found ID={} ISO={}", userData.Country, country.Id, country.ISOName);

        var language = await GetLanguageIdByIsoCode(userData.DefaultLanguage);

        _logger.LogInformation(
            "Language provided {}; language found ID={} ISO={}",
            userData.DefaultLanguage,
            language.Id,
            language.IsoCode
        );

        var user = new User
                   {
                       IdentityServerId = userData.IdentityServerId,
                       Email = userData.Email?.ToLower(),
                       AppleId = userData.AppleId,
                       GoogleId = userData.GoogleId,
                       PhoneNumber = userData.PhoneNumber,
                       CreatorPermissionLevel = [],
                       AnalyticsEnabled = userData.AnalyticsEnabled,
                       DataCollection = userData.AllowDataCollection,
                       CreatedTime = DateTime.UtcNow,
                       ModifiedTime = DateTime.UtcNow
                   };

        var group = new Group
                    {
                        NickName = userData.NickName,
                        BirthDate = userData.BirthDate,
                        TaxationCountryId = country.Id,
                        DefaultLanguageId = language.Id,
                        CharacterAccess = CharacterAccess.ForFriends,
                        CreatedTime = DateTime.UtcNow,
                        ModifiedTime = DateTime.UtcNow,
                        ParentalConsent = new ParentalConsent()
                    };

        SetGroupMinorData(group, country, userData.IsMinor);

        var userGroup = new UserAndGroup {Group = group, User = user};
        var userGroupPublic = new UserAndGroup {GroupId = Constants.PublicAccessGroupId, User = user};

        user.MainGroup = group;

        await writeDb.UserAndGroup.AddAsync(userGroup);
        await writeDb.UserAndGroup.AddAsync(userGroupPublic);
        await writeDb.SaveChangesAsync();

        _logger.LogInformation("Db data: Id={} countryId={} languageId={}", group.Id, group.TaxationCountryId, group.DefaultLanguageId);
    }

    public async Task UpdateUser(UserUpdateModel userData)
    {
        var country = await GetCountryByIsoCode(userData.Country);

        _logger.LogInformation("Country provided {}; country found ID={} ISO={}", userData.Country, country.Id, country.ISOName);

        var language = await GetLanguageIdByIsoCode(userData.DefaultLanguage);

        _logger.LogInformation(
            "Language provided {}; language found ID={} ISO={}",
            userData.DefaultLanguage,
            language.Id,
            language.IsoCode
        );

        var user = await writeDb.User.Include(e => e.MainGroup).FirstOrDefaultAsync(e => e.IdentityServerId == userData.IdentityServerId);

        user.Email = userData.Email;
        user.AppleId = userData.AppleId;
        user.GoogleId = userData.GoogleId;
        user.PhoneNumber = userData.PhoneNumber;
        user.HasPassword = userData.HasPassword;
        user.MainGroup.NickName = userData.NickName;
        user.MainGroup.BirthDate = userData.BirthDate;
        user.MainGroup.TaxationCountryId = country.Id;
        user.MainGroup.DefaultLanguageId = language.Id;
        user.MainGroup.IsTemporary = false;

        SetGroupMinorData(user.MainGroup, country, userData.IsMinor);

        await writeDb.SaveChangesAsync();
    }

    public async Task UpdateUser(User user, string countryIso, string languageIso, bool isMinor)
    {
        var country = await GetCountryByIsoCode(countryIso);

        _logger.LogInformation("Country provided {}; country found ID={} ISO={}", countryIso, country.Id, country.ISOName);

        var language = await GetLanguageIdByIsoCode(languageIso);

        _logger.LogInformation("Language provided {}; language found ID={} ISO={}", languageIso, language.Id, language.IsoCode);

        user.MainGroup.TaxationCountryId = country.Id;
        user.MainGroup.DefaultLanguageId = language.Id;

        SetGroupMinorData(user.MainGroup, country, isMinor);

        await writeDb.SaveChangesAsync();
    }

    public async Task<MainDbClaimData> GetClaimsDataAsync(string identityServerUserId)
    {
        var id = Guid.Parse(identityServerUserId);

        var response = await writeDb.User.Where(x => x.IdentityServerId == id)
                                    .Select(
                                         e => new MainDbClaimData
                                              {
                                                  UserId = e.Id,
                                                  PrimaryGroupId = e.MainGroupId,
                                                  MainCharacterId = e.MainCharacterId,
                                                  IsFeatured = e.IsFeatured,
                                                  IsStarCreator = e.MainGroup.IsStarCreator,
                                                  IsOnboardingCompleted = e.MainGroup.IsOnboardingCompleted,
                                                  RegisteredWithAppleId = !string.IsNullOrWhiteSpace(e.AppleId),
                                                  CreatorPermissionLevels = e.CreatorPermissionLevel ?? Array.Empty<long>()
                                              }
                                     )
                                    .FirstOrDefaultAsync();

        var roleIds = await writeDb.UserRole.Where(ur => ur.GroupId == response.PrimaryGroupId).Select(e => e.RoleId).ToArrayAsync();

        response.IsEmployee = roleIds.Length != 0;
        response.IsQA = roleIds.Contains(KnowUserRoles.QaRoleId);
        response.IsModerator = roleIds.Contains(KnowUserRoles.AdminRoleId);

        return response;
    }

    public Task<IDbContextTransaction> BeginMainDbTransactionAsync()
    {
        return writeDb.BeginTransaction();
    }

    public Task<IDbContextTransaction> BeginAuthDbTransactionAsync()
    {
        return authDb.Database.BeginTransactionAsync();
    }

    public async Task<bool> IsGroupBlockedForAuthUser(string identityServerId)
    {
        if (string.IsNullOrWhiteSpace(identityServerId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identityServerId));

        var id = Guid.Parse(identityServerId);

        var user = await writeDb.User.Where(u => u.IdentityServerId == id)
                                .Where(e => !e.MainGroup.IsBlocked && e.MainGroup.DeletedAt == null)
                                .Select(u => new {u.MainGroupId})
                                .FirstOrDefaultAsync();

        return user == null;
    }

    public IQueryable<User> GetUserByGroupId(long groupId)
    {
        return writeDb.User.Where(u => u.MainGroupId == groupId);
    }

    public Task<User> GetUserByAppleId(string appleId)
    {
        return writeDb.User.Where(u => u.AppleId == appleId).OrderByDescending(u => u.Id).FirstOrDefaultAsync();
    }

    public Task<User> GetUserByGoogleId(string googleId)
    {
        return writeDb.User.Where(u => u.GoogleId == googleId).OrderByDescending(u => u.Id).FirstOrDefaultAsync();
    }

    public IQueryable<Group> GetGroupByName(string name)
    {
        return writeDb.Group.Where(g => g.NickName == name);
    }

    public IQueryable<User> AllUsers()
    {
        return writeDb.User;
    }

    public IQueryable<ApplicationUser> GetAuthDbUsersByUsername(string username)
    {
        username = username.ToUpperInvariant();
        return authDb.Users.Where(e => e.NormalizedUserName == username);
    }

    public IQueryable<Country> GetCountries()
    {
        return writeDb.Country;
    }

    public IQueryable<Group> GetGroupById(long groupId)
    {
        return writeDb.Group.Where(g => g.Id == groupId);
    }

    public IQueryable<Group> GetGroupByIdentityServerId(string identityServerId)
    {
        if (string.IsNullOrWhiteSpace(identityServerId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(identityServerId));

        var guid = Guid.Parse(identityServerId);
        return writeDb.User.Where(u => u.IdentityServerId == guid).Select(e => e.MainGroup);
    }

    public Task<bool> IsEmailRegistered(string email)
    {
        return writeDb.User.AnyAsync(u => u.Email == email);
    }

    public Task<bool> IsPhoneRegistered(string phoneNumber)
    {
        return writeDb.User.AnyAsync(u => u.PhoneNumber == phoneNumber);
    }

    public Task<bool> IsAppleIdRegistered(string appleId)
    {
        return writeDb.User.AnyAsync(u => u.AppleId == appleId);
    }

    public Task<bool> IsGoogleIdRegistered(string googleId)
    {
        return writeDb.User.AnyAsync(u => u.GoogleId == googleId);
    }

    public Task<bool> IsNicknameUsed(string nickname)
    {
        return writeDb.User.AnyAsync(u => u.MainGroup.NickName.ToLower() == nickname.ToLower());
    }

    public Task<bool> AuthUserHasPassword(string identityServerId)
    {
        return authDb.Users.AnyAsync(e => e.Id == identityServerId && e.PasswordHash != null);
    }

    public Task<string> LookupEmailByAppleToken(string appleId)
    {
        return writeDb.AppleSignInEmail.Where(a => a.AppleId == appleId).Select(e => e.Email).FirstOrDefaultAsync();
    }

    public async Task StoreEmailForAppleId(string appleId, string email)
    {
        if (string.IsNullOrWhiteSpace(appleId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(appleId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(email));

        await using var transaction = await writeDb.BeginTransaction();

        var existing = await writeDb.AppleSignInEmail.FirstOrDefaultAsync(e => e.AppleId == appleId);
        if (existing == null)
            writeDb.AppleSignInEmail.Add(new AppleSignInEmail {AppleId = appleId, CreatedAt = DateTime.UtcNow, Email = email});
        else
            existing.Email = email;

        await writeDb.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    public Task<bool> IsParentEmailNonUnique(long groupId, string parentEmail)
    {
        if (string.IsNullOrWhiteSpace(parentEmail))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(parentEmail));

        return writeDb.User.Where(u => u.Email.ToLower() == parentEmail.ToLower() && u.MainGroupId != groupId).AnyAsync();
    }

    public async Task SetParentEmail(long groupId, string parentEmail, bool isParentAgeValidated)
    {
        if (string.IsNullOrWhiteSpace(parentEmail))
            parentEmail = null;

        var user = await writeDb.User.FirstOrDefaultAsync(u => u.MainGroupId == groupId);
        var group = await writeDb.Group.FirstOrDefaultAsync(g => g.Id == groupId);

        if (user != null)
            user.Email = parentEmail?.ToLower();

        if (group is {IsParentalConsentValidated: false}) // If quiz passed before do not reset validation on email assigning
            group.IsParentalConsentValidated = isParentAgeValidated;

        await writeDb.SaveChangesAsync();
    }

    public async Task AddInitialFriend(long currentGroupId, string freverOfficialEmail)
    {
        if (string.IsNullOrWhiteSpace(freverOfficialEmail))
            throw new ArgumentNullException(nameof(freverOfficialEmail));

        var group = await writeDb.User.Where(e => e.Email.ToLower().Equals(freverOfficialEmail.ToLower()))
                                 .Select(e => new {e.MainGroupId})
                                 .FirstOrDefaultAsync();
        if (group is null)
        {
            _logger.LogInformation("FreverOfficial group is not found by email {FreverOfficialEmail}", freverOfficialEmail);
            return;
        }

        var friends = new[]
                      {
                          new Follower
                          {
                              FollowingId = currentGroupId,
                              FollowerId = group.MainGroupId,
                              State = FollowerState.Following,
                              IsMutual = true,
                              Time = DateTime.UtcNow
                          },
                          new Follower
                          {
                              FollowingId = group.MainGroupId,
                              FollowerId = currentGroupId,
                              State = FollowerState.Following,
                              IsMutual = true,
                              Time = DateTime.UtcNow
                          }
                      };

        await writeDb.Follower.AddRangeAsync(friends);
        await writeDb.SaveChangesAsync();
        foreach (var follower in friends)
            await snsMessagingService.PublishSnsMessageForGroupFollowed(
                follower.FollowingId,
                follower.FollowerId,
                follower.IsMutual,
                follower.Time
            );
    }

    public Task SaveChanges()
    {
        return writeDb.SaveChangesAsync();
    }

    private async Task<Language> GetLanguageIdByIsoCode(string isoCode)
    {
        var language = await writeDb.Language.FirstOrDefaultAsync(c => c.IsoCode == isoCode.ToLower() || c.ISO2Code == isoCode.ToLower());

        if (language == null)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid language code", "InvalidLanguageCode");

        return language;
    }

    private async Task<Country> GetCountryByIsoCode(string isoCode)
    {
        var inputCountry = isoCode.ToLower().Trim();

        var country = inputCountry.Length == 2
                          ? await writeDb.Country.FirstOrDefaultAsync(l => l.ISO2Code.ToLower() == inputCountry)
                          : await writeDb.Country.FirstOrDefaultAsync(l => l.ISOName.ToLower() == inputCountry);

        if (country == null)
            throw AppErrorWithStatusCodeException.BadRequest("Invalid country code", "InvalidCountryCode");

        return country;
    }

    private static void SetGroupMinorData(Group group, Country country, bool isMinor)
    {
        group.IsMinor = isMinor;
        group.BirthDate = isMinor ? DateTime.UtcNow : group.BirthDate;
        group.NickName = isMinor && country.StrictCoppaRules ? null : group.NickName;
        group.IsParentalConsentValidated = !isMinor || !country.ExtendedParentAgeValidation;
        group.ParentalConsent.AllowInAppPurchase = !country.ExtendedParentAgeValidation;
    }
}