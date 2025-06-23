namespace Frever.ClientService.Contract.Metadata;

public class MakeUpCategoryDto
{
    public long Id { get; set; }
    public string Name { get; set; }
    public bool IsPreset { get; set; }
    public int SortOrder { get; set; }
}