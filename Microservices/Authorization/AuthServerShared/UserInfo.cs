using System;
using System.Linq;
using System.Security.Claims;

namespace AuthServerShared;

/// <summary>
///     Represent user/caller related information like  claims ,id etc.
/// </summary>
public class UserInfo(
    long userId,
    long groupId,
    bool isFeatured,
    bool isStarCreator,
    long[] creatorAccessLevels,
    string[] accessScopes
)
{
    public long UserMainGroupId { get; } = groupId;
    public long UserId { get; } = userId;
    public bool IsFeatured { get; } = isFeatured;
    public long[] CreatorAccessLevels { get; } = creatorAccessLevels ?? [];
    public string[] AccessScopes { get; } = accessScopes ?? [];
    public bool IsStarCreator { get; } = isStarCreator;

    public static implicit operator long(UserInfo user)
    {
        return user.UserMainGroupId;
    }
}

public static class UserInfoFabric
{
    public static UserInfo ConvertToUserInfo(ClaimsPrincipal user)
    {
        try
        {
            if (user.FindFirst(Claims.PrimaryGroupId) == null)
                return null;

            var groupId = long.Parse(user.FindFirst(Claims.PrimaryGroupId).Value);
            var userId = long.Parse(user.FindFirst(Claims.UserId).Value);

            var isFeatured = false;
            if (user.HasClaim(x => x.Type == Claims.IsFeatured))
                isFeatured = bool.Parse(user.FindFirst(Claims.IsFeatured).Value);

            var isStarCreator = false;
            if (user.HasClaim(x => x.Type == Claims.IsStarCreator))
                isStarCreator = bool.Parse(user.FindFirst(Claims.IsStarCreator).Value);

            var creatorLevels = user.FindAll(Claims.CreatorPermissionLevels).Select(c => c.Value).Select(long.Parse).ToArray();
            var accessScopes = user.FindAll(Claims.AccessScopes).Select(c => c.Value).ToArray();

            return new UserInfo(
                userId,
                groupId,
                isFeatured,
                isStarCreator,
                creatorLevels,
                accessScopes
            );
        }
        catch (Exception)
        {
            throw new Exception("User has no one( or few) required claim.");
        }
    }
}