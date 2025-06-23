using System;
using System.Collections.Generic;
using Common.Models.Database.Interfaces;
using Common.Models.Files;

namespace Frever.Shared.MainDb.Entities;

public class Group : IEntity, ITimeChangesTrackable, IFileMetadataConfigRoot
{
    public const int PublicGroupId = 1;

    public long Id { get; set; }
    public string NickName { get; set; }
    public long? TaxationCountryId { get; set; }
    public long DefaultLanguageId { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime ModifiedTime { get; set; }


    private DateTime? _birthDate;

    public DateTime? BirthDate
    {
        get => _birthDate;
        set
        {
            if (value.HasValue)
            {
                var startOfMonth = new DateTime(value.Value.Year, value.Value.Month, 1);
                _birthDate = startOfMonth.AddMonths(1).AddDays(-1);
            }
            else
            {
                _birthDate = null;
            }
        }
    }

    public DateTime? DeletedAt { get; set; }
    public long? Gender { get; set; }
    public long? ToplistPosition { get; set; }
    public CharacterAccess CharacterAccess { get; set; }
    public int CreatorScoreBadge { get; set; }
    public long CreatorScore { get; set; }
    public long? CreatorRank { get; set; }
    public int TotalLikes { get; set; }
    public int TotalVideos { get; set; }
    public int TotalFollowers { get; set; }
    public string Bio { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsStarCreator { get; set; }
    public bool IsStarCreatorCandidate { get; set; }

    public bool IsOnboardingCompleted { get; set; }

    //TODO: remove later
    public bool IsTemporary { get; set; }
    public bool IsOnWatchList { get; set; }
    public bool IsOfficial { get; set; }
    public bool IsCommunityBuilder { get; set; }
    public bool IsMinor { get; set; }
    public bool IsParentalConsentValidated { get; set; }
    public ParentalConsent ParentalConsent { get; set; }
    public ICollection<AppsFlyerId> AppsFlyerIds { get; set; }
    public DateTime? NickNameUpdatedAt { get; set; }
    public bool DisableWatermark { get; set; }
    public FileMetadata[] Files { get; set; }

    public virtual Gender GenderNavigation { get; set; }
    public virtual Country TaxationCountry { get; set; }
    public virtual Language DefaultLanguage { get; set; }
    public virtual ICollection<BlockedUser> BlockedUsers { get; set; }
    public virtual ICollection<BlockedUser> BlockedByUsers { get; set; }
    public virtual ICollection<Brand> Brand { get; set; }
    public virtual ICollection<Follower> FollowerFollowerNavigation { get; set; }
    public virtual ICollection<Follower> FollowerFollowing { get; set; }
    public virtual ICollection<FollowerHistory> FollowerHistoryFollower { get; set; }
    public virtual ICollection<FollowerHistory> FollowerHistoryFollowing { get; set; }
    public virtual ICollection<Song> Song { get; set; }
    public virtual ICollection<User> User { get; set; }
    public virtual ICollection<UserAndGroup> UserAndGroup { get; set; }
    public virtual ICollection<UserSound> UserSound { get; set; }
    public virtual ICollection<Video> Video { get; set; }
    public virtual ICollection<VideoGroupTag> VideoGroupTag { get; set; }
}

public class ParentalConsent
{
    public static readonly ParentalConsent AllowAll = new()
                                                      {
                                                          AccessCamera = true,
                                                          AccessMicrophone = true,
                                                          AllowCaptions = true,
                                                          AllowChat = true,
                                                          AllowComments = true,
                                                          AudioUploads = true,
                                                          ImageUploads = true,
                                                          PushNotifications = true,
                                                          ShareContacts = true,
                                                          VideoUploads = true,
                                                          AllowVideoDescription = true,
                                                          AllowCrewCreation = true,
                                                          AllowInAppPurchase = true
                                                      };

    public static readonly ParentalConsent DenyAll = new()
                                                     {
                                                         AccessCamera = false,
                                                         AccessMicrophone = false,
                                                         AllowCaptions = false,
                                                         AllowChat = false,
                                                         AllowComments = false,
                                                         AudioUploads = false,
                                                         ImageUploads = false,
                                                         PushNotifications = false,
                                                         ShareContacts = false,
                                                         VideoUploads = false,
                                                         AllowVideoDescription = false,
                                                         AllowCrewCreation = false,
                                                         AllowInAppPurchase = false
                                                     };

    public bool AllowChat { get; set; }             /* Checked */
    public bool AllowComments { get; set; }         /* Checked */
    public bool AllowVideoDescription { get; set; } /* Checked */
    public bool AllowCaptions { get; set; }         /* Checked */
    public bool AudioUploads { get; set; }          /* Checked */
    public bool VideoUploads { get; set; }          /* Checked */
    public bool ImageUploads { get; set; }
    public bool AccessMicrophone { get; set; }
    public bool AccessCamera { get; set; }
    public bool ShareContacts { get; set; }
    public bool PushNotifications { get; set; }
    public bool AllowCrewCreation { get; set; }
    public bool AllowInAppPurchase { get; set; } = true;
}

public class AppsFlyerId
{
    public string Id { get; set; }
    public int Platform { get; set; }
}