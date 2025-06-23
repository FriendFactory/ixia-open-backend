using Frever.Shared.MainDb.Entities;

namespace AuthServer.Contracts;

public class ConfigureParentalConsentRequest
{
    public ParentalConsent ParentalConsent { get; set; }
}