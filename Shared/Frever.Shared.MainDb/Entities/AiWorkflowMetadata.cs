namespace Frever.Shared.MainDb.Entities;

public class AiWorkflowMetadata
{
    public long Id { get; set; }
    public string Key { get; set; }
    public required string AiWorkflow { get; set; }
    public string Description { get; set; }
    public bool RequireBillingUnits { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public required int HardCurrencyPrice { get; set; }
    public int? EstimatedLoadingTimeSec { get; set; }
}