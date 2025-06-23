namespace Common.Infrastructure.RegexUtils;

public static class RegexPatterns
{
    public const string Mentions = @"(?<=@)\d+";
    public const string Hashtags = @"(?<=#)\w{1,25}";
}