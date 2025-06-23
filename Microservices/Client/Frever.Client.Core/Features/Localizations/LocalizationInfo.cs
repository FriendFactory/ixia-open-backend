using System.Collections.Generic;

namespace Frever.Client.Core.Features.Localizations;

public class LocalizationInfo
{
    public string Key { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Values { get; set; }
    public bool IsStartupItem { get; set; }
}