using System;

namespace Frever.Shared.MainDb.Entities;

public class UserActivity
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public DateTime OccurredAt { get; set; }
    public int? Xp { get; set; }
    public long? RefVideoId { get; set; }
    public long? RefTaskId { get; set; }
    public long? RefLevelId { get; set; }
    public long? RefGroupId { get; set; }
    public UserActionType ActionType { get; set; }
    public int? StreakLength { get; set; }
    public long? DailyQuestId { get; set; }
    public long? RefActorGroupId { get; set; }
    public long? RefRelatedUserActivityId { get; set; }
    public long? SeasonId { get; set; }
    public int? UserLevel { get; set; }
    public long? SeasonLevelRewardId { get; set; }
    public long? SeasonQuestId { get; set; }
    public long? CreatorLevelRewardId { get; set; }
    public long? BattleRewardId { get; set; }
    public long? OnboardingRewardId { get; set; }
    public long? CrewCompetitionId { get; set; }
    public long? CrewCompetitionRewardId { get; set; }
    public int? Value { get; set; }
}