namespace Frever.ClientService.Contract.Locales;

public class CountryDto
{
    public long Id { get; set; }
    public string Iso2Code { get; set; }
    public string Iso3Code { get; set; }
    public bool EnableMusic { get; set; }
    public int AgeOfConsent { get; set; }
    public bool StrictCoppaRules { get; set; }
}