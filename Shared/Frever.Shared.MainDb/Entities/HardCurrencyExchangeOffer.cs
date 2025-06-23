namespace Frever.Shared.MainDb.Entities;

public class HardCurrencyExchangeOffer
{
    public long Id { get; set; }
    public int HardCurrencyRequired { get; set; }
    public int SoftCurrencyGiven { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    public string Title { get; set; }
}