namespace Frever.AdminService.Core.Services.AssetTransaction;

public class IncreaseCurrencySupplyInfo
{
    public int? SoftCurrencyAmount { get; set; }
    public int? HardCurrencyAmount { get; set; }
    public long[] GroupIds { get; set; }
}