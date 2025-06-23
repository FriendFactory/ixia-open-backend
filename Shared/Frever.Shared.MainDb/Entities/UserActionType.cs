namespace Frever.Shared.MainDb.Entities;

public enum UserActionType
{
    WatchVideo = 1,
    WatchVideoStreak = 10,
    CompleteTask = 2,
    OriginalVideoCreated = 3,
    TemplateVideoCreated = 4,
    LikeVideo = 5,
    LikeVideoStreak = 9,
    Login = 6,
    LoginStreak = 7,
    OriginalVideoCreationStreak = 8,
    LikeReceived = 11,
    LikeReceivedStreak = 12,
    DailyQuestRewardClaimed = 13,
    LevelUpRewardClaimed = 14,
    SeasonQuestRewardClaimed = 15,
    CreatorLevelRewardClaimed = 16,
    BattleRewardClaimed = 17,
    InvitationCodeRewardClaimed = 18,
    UpdateUserXp = 19,
    OnboardingRewardClaimed = 20,
    CrewRewardClaimed = 21,
    BattleResultReady = 22,
    PurchaseSeasonLevel = 23,
    PublishedVideoShare = 24,
    VideoRaterRewardClaimed = 25,
    RatedVideoRewardClaimed = 26,
    RatingReceived = 27
}