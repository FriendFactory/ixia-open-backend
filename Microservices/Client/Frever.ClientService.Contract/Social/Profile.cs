using System.Collections.Generic;
using Common.Models.Files;

namespace Frever.ClientService.Contract.Social;

public class Profile
{
    public long MainGroupId { get; set; }
    public string NickName { get; set; }
    public ProfileKpi KPI { get; set; } = new();
    public bool YouFollowUser { get; set; }
    public bool UserFollowsYou { get; set; }
    public string Bio { get; set; }
    public Dictionary<string, string> BioLinks { get; set; }
    public bool IsNewFriend { get; set; }
    public FileMetadata[] Files { get; set; }
}