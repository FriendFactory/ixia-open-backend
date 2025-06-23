using System;
using System.Collections.Generic;
using Common.Models.Files;
using Frever.ClientService.Contract.Social;

namespace Frever.Client.Core.Features.Social.Profiles;

public class ProfileInternal : IFileMetadataOwner
{
    public long MainGroupId { get; set; }
    public string NickName { get; set; }
    public ProfileKpi KPI { get; set; } = new();
    public bool YouFollowUser { get; set; }
    public bool UserFollowsYou { get; set; }
    public string Bio { get; set; }
    public Dictionary<string, string> BioLinks { get; set; }
    public bool IsNewFriend { get; set; }
    public DateTime CreatedTime { get; set; }
    public long Id { get; set; }
    public FileMetadata[] Files { get; set; }
}

public static class ProfileMapper
{
    public static Profile FromInternal(ProfileInternal profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new Profile
               {
                   Bio = profile.Bio,
                   BioLinks = profile.BioLinks,
                   NickName = profile.NickName,
                   KPI = profile.KPI,
                   MainGroupId = profile.MainGroupId,
                   UserFollowsYou = profile.UserFollowsYou,
                   YouFollowUser = profile.YouFollowUser,
                   IsNewFriend = profile.IsNewFriend,
                   Files = profile.Files
               };
    }
}