using System.Collections.Generic;
using Common.Models.Files;

namespace Frever.Client.Core.Features.Social.MyProfileInfo;

public class UpdateProfileRequest
{
    public string Bio { get; set; }

    public Dictionary<string, string> BioLinks { get; set; }

    public FileMetadata[] Files { get; set; }
}