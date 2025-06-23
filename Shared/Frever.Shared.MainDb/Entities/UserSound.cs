using System;
using Common.Models.Database.Interfaces;
using Common.Models.Files;

namespace Frever.Shared.MainDb.Entities;

public class UserSound : IGroupAccessible, ITimeChangesTrackable, IFileMetadataConfigRoot
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime ModifiedTime { get; set; }
    public long? Size { get; set; }
    public int Duration { get; set; }
    public string CopyrightCheckResults { get; set; }
    public bool? ContainsCopyrightedContent { get; set; }
    public string Name { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int UsageCount { get; set; }
    public FileMetadata[] Files { get; set; }
    public virtual Group Group { get; set; }
}