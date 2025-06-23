namespace Frever.Shared.MainDb.Entities;

public class AiMakeUpCategory
{
    public long Id { get; set; }
    public string Name { get; set; }
    public bool IsPreset { get; set; }
    public string Workflow { get; set; }
    public int SortOrder { get; set; }
}