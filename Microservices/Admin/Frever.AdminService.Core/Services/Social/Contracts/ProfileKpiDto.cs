namespace Frever.AdminService.Core.Services.Social.Contracts;

public class ProfileKpiDto
{
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public long VideoLikesCount { get; set; }
    public int PublishedVideoCount { get; set; }
    public int TotalVideoCount { get; set; }
    public int TaggedInVideoCount { get; set; }
    public int TotalDraftsCount { get; set; }
    public int TotalLevelsCount { get; set; }
    public int CharacterCount { get; set; }
    public int LevelCount { get; set; }
    public int OutfitCount { get; set; }
    public int VideoClipsCount { get; set; }
    public int UserSoundsCount { get; set; }
    public int UserPhotosCount { get; set; }
    public int HardCurrency { get; set; }
    public int SoftCurrency { get; set; }
    public int XPScore { get; set; }
    public int TotalPurchases { get; set; }
}