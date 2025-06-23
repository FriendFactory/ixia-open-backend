using System;
using System.Collections.Generic;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class User : IEntity, ITimeChangesTrackable
{
    public User()
    {
        Song = new HashSet<Song>();
        UserAndGroup = new HashSet<UserAndGroup>();
    }

    public long Id { get; set; }
    public Guid IdentityServerId { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime ModifiedTime { get; set; }
    public long[] CreatorPermissionLevel { get; set; }
    public long? MainCharacterId { get; set; }
    public bool? DataCollection { get; set; }
    public long MainGroupId { get; set; }
    public bool IsFeatured { get; set; }
    public bool AnalyticsEnabled { get; set; }
    public string AppleId { get; set; }
    public string GoogleId { get; set; }
    public bool HasPassword { get; set; }

    public virtual Group MainGroup { get; set; }
    public virtual ICollection<Song> Song { get; set; }
    public virtual ICollection<UserAndGroup> UserAndGroup { get; set; }
}