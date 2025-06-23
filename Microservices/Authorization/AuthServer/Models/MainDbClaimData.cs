namespace AuthServer.Models;

public class MainDbClaimData
{
    public long UserId { get; set; }
    public long PrimaryGroupId { get; set; }
    public long? MainCharacterId { get; set; }
    public long[] CreatorPermissionLevels { get; set; }
    public bool IsQA { get; set; }
    public bool IsModerator { get; set; }
    public bool IsEmployee { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsStarCreator { get; set; }
    public bool IsOnboardingCompleted { get; set; }
    public bool RegisteredWithAppleId { get; set; }
}