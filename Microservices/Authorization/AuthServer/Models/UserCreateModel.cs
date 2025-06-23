using System;

namespace AuthServer.Models;

public class UserCreateModel
{
    public bool AllowDataCollection { get; set; }
    public bool AnalyticsEnabled { get; set; }
    public string AppleId { get; set; }
    public DateTime? BirthDate { get; set; }
    public string Email { get; set; }
    public string GoogleId { get; set; }
    public Guid IdentityServerId { get; set; }
    public string NickName { get; set; }
    public string PhoneNumber { get; set; }
    public string DefaultLanguage { get; set; }
    public string Country { get; set; }
    public bool IsMinor { get; set; }
    public bool IsTemporary { get; set; }
}