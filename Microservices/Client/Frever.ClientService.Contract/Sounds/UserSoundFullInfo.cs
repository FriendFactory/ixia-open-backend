using System;
using Common.Models.Files;
using Frever.ClientService.Contract.Social;

namespace Frever.ClientService.Contract.Sounds;

public class UserSoundFullInfo : IFileMetadataOwner
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Duration { get; set; }
    public DateTime CreatedTime { get; set; }
    public int UsageCount { get; set; }
    public bool IsFavorite { get; set; }
    public GroupShortInfo Owner { get; set; }
    public FileMetadata[] Files { get; set; }
}