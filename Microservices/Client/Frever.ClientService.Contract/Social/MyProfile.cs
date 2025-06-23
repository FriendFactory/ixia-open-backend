using System;
using System.Collections.Generic;
using Frever.Shared.MainDb.Entities;

namespace Frever.ClientService.Contract.Social;

public class MyProfile
{
    public string Nickname { get; set; }
    public long? TaxationCountryId { get; set; }
    public bool IsEmployee { get; set; }
    public bool? DataCollectionEnabled { get; set; }
    public DateTime? BirthDate { get; set; }
    public bool AnalyticsEnabled { get; set; }
    public UserBalance UserBalance { get; set; }
    public string Bio { get; set; }
    public Dictionary<string, string> BioLinks { get; set; }
    public string DetectedLocationCountry { get; set; }
    public bool AdvertisingTrackingEnabled { get; set; }
    public ParentalConsent ParentalConsent { get; set; }
    public bool IsParentAgeValidated { get; set; }
    public bool IsInAppPurchaseAllowed { get; set; }
    public string EmailRedacted { get; set; }
    public bool IsNicknameChangeAllowed { get; set; }
    public bool IsOnboardingCompleted { get; set; }
    public DateTime? UsernameUpdateAvailableOn { get; set; }
    public bool HasDefaultUsername { get; set; }
    public bool HasUpdatedCredentials { get; set; }
}