namespace Frever.AdminService.Core.Services.HashtagModeration.Contracts;

public class HashtagInfo
{
    public long Id { get; set; }
    public string Name { get; set; }
    public long ViewsCount { get; set; }
    public long VideoCount { get; set; }
    public long ChallengeSortOrder { get; set; }
}