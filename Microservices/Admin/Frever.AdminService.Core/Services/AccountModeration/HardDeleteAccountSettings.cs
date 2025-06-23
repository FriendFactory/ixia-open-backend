namespace Frever.AdminService.Core.Services.AccountModeration;

public class HardDeleteAccountSettings
{
    public int DeletedDaysAgo { get; set; } = 25;
    public string DeletionErrorEmailRecipients { get; set; }
    public string EnvironmentInfo { get; set; }
}