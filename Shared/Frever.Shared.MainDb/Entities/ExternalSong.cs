using System;
using Common.Models.Database.Interfaces;

namespace Frever.Shared.MainDb.Entities;

public class ExternalSong : IEntity, IAdminAsset, ITimeChangesTrackable
{
    public string SongName { get; set; }
    public string ArtistName { get; set; }
    public int SortOrder { get; set; }
    public int? ChallengeSortOrder { get; set; }
    public bool IsDeleted { get; set; }
    public string Isrc { get; set; }
    public string[] ExcludedCountries { get; set; }
    public DateTime? LastLicenseStatusCheckAt { get; set; }
    public DateTime? NotClearedSince { get; set; }
    public int? SpotifyPopularity { get; set; }
    public DateTime? SpotifyPopularityLastUpdate { get; set; }
    public bool IsManuallyDeleted { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime ModifiedTime { get; set; }
    public bool HasBeenUsedInVideo { get; set; }
    public int UsageCount { get; set; }

    public long Id { get; set; }
}