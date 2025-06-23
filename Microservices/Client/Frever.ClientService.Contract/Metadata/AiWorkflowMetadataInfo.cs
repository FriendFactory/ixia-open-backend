namespace Frever.ClientService.Contract.Metadata;

public class AiWorkflowMetadataInfo
{
    public long Id { get; set; }
    public string Key { get; set; }
    public int UnitPrice { get; set; }
    public int? EstimatedLoadingTimeSec { get; set; }
}