namespace Common.Models;

public static class Constants
{
    public const long PublicAccessGroupId = 1;

    public const string FallbackLocalizationCode = "eng";
    public static readonly string[] StaticHeaders = ["Key", "Type", "Desc"];

    public const string Wildcard = "*";
    public const string FallbackCountryCode = "swe";
    public const string FallbackLanguageCode = "swe";

    public const string DefaultAssetUnityVersion = "2019";

    public const int UsernameUpdateIntervalDays = 30;

    public const int RewardedShareCount = 3;
    public const int ShareVideoSoftCurrency = 100;

    public const int InitialAccountSoftCurrency = 5000;
    public const int InitialAccountHardCurrency = 50;

    public const string FilesFolder = "Files";
    public const string TemporaryFolder = "Preloaded";
}