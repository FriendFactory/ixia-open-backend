namespace Frever.Shared.MainDb.Entities;

public enum AssetStoreTransactionType
{
    LevelCreation = 1,
    CharacterCreation = 2,
    OutfitCreation = 3,
    InitialAccountBalance = 4,
    InAppPurchase = 5,
    Achievement = 6,
    HelicopterMoney = 7,
    LevelUp = 8,
    TaskCompletion = 9,
    UserStatusPayout = 10,
    SystemExpense = 11, // For double bookkeeping
    SystemIncome = 12,  // For double bookkeeping
    DailyQuest = 13,
    PremiumPurchase = 14,
    HardCurrencyExchange = 15,
    InAppPurchaseRefund = 16,
    CreatorLevelUp = 17,
    Invitation = 18,
    OnboardingReward = 19,
    CrewReward = 20,
    DirectPurchase = 21,
    PurchaseLevel = 22,
    PublishedVideoShare = 23,
    VideoRating = 24,
    AiWorkflowRun = 25,
    AiWorkflowRunErrorRefund = 26,
    DailyTokenRefill = 27,
    DailyTokenBurnout = 28,
    MonthlyTokenRefill = 29,
    MonthlyTokenBurnout = 30
}