using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Common.IntegrationTesting.Data;

public static class UserData
{
    public static async Task<User> WithUserAndGroup(this DataEnvironment data, UserAndGroupCreateParams userInput = null)
    {
        userInput ??= new UserAndGroupCreateParams();
        NormalizeUser(userInput);
        var users = await data.WithEntity<User>("create-user-account", userInput);

        return users.Single();
    }

    public static async Task<User[]> WithSystemUserAndGroup(this DataEnvironment dataEnv)
    {
        return await dataEnv.WithUsersAndGroups(
                   new UserAndGroupCreateParams {Email = "xxxxxxxxx", CountryIso3 = "swe", LanguageIso3 = "swe"},
                   new UserAndGroupCreateParams {Email = "xxxxxxxxx", CountryIso3 = "swe", LanguageIso3 = "swe"},
                   new UserAndGroupCreateParams {Email = "xxxxxxxxx", CountryIso3 = "swe", LanguageIso3 = "swe"}
               );
    }

    public static async Task<User[]> WithUsersAndGroups(this DataEnvironment data, params UserAndGroupCreateParams[] userInput)
    {
        ArgumentNullException.ThrowIfNull(userInput);
        foreach (var user in userInput)
            NormalizeUser(user);

        return await data.WithEntityCollection<User>("create-user-account", userInput);
    }

    public static async Task WithSocialRelations(this DataEnvironment data, params FollowRelation[] followRelations)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(followRelations);

        var input = followRelations.SelectMany(
                                        a => a.FollowGroups,
                                        (r, i) => new {r.GroupId, FollowsGroupId = i.GroupId, Time = i.FollowTime}
                                    )
                                   .ToArray();

        foreach (var item in input)
            await data.WithScript("create-social-relation", item);
    }

    public static async Task WithAdminPermissions(this DataEnvironment data, long groupId, params string[] adminRoles)
    {
        ArgumentNullException.ThrowIfNull(adminRoles);

        var allRoles = await data.Db.Role.ToArrayAsync();
        var roleName = adminRoles.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim().ToLower()).ToHashSet();

        var rolesToAdd = allRoles.Where(r => roleName.Contains(r.Name.Trim().ToLowerInvariant())).ToArray();

        data.Db.UserRole.AddRange(rolesToAdd.Select(r => new UserRole {GroupId = groupId, RoleId = r.Id, CreatedAt = DateTime.UtcNow}));

        await data.Db.SaveChangesAsync();
    }

    private static void NormalizeUser(UserAndGroupCreateParams userInput)
    {
        ArgumentNullException.ThrowIfNull(userInput);

        var uuid = Guid.NewGuid().ToString("N");

        userInput.NickName ??= $"test-user-{uuid}";
        userInput.BirthDate ??= new DateTime(1990, 10, 01).AddDays(Random.Shared.Next(10, 1000));
        userInput.Email ??= $"{userInput.NickName}@frever-test.com";
    }
}

public class UserAndGroupCreateParams
{
    public string NickName { get; set; }
    public string CountryIso3 { get; set; } = "swe";
    public string LanguageIso3 { get; set; } = "swe";
    public DateTime? BirthDate { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public long? MainCharacterId { get; set; }
    public DateTime? LastLogin { get; set; }
    public bool IsStarCreator { get; set; }
}

public class FollowRelation
{
    public long GroupId { get; set; }

    public FollowingInfo[] FollowGroups { get; set; } = [];
}

public class FollowingInfo
{
    public long GroupId { get; set; }

    public DateTime FollowTime { get; set; }
}