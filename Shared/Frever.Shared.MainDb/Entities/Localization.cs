namespace Frever.Shared.MainDb.Entities;

public class Localization
{
    public string Key { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public string Value { get; set; }
    public bool IsStartupItem { get; set; }
}