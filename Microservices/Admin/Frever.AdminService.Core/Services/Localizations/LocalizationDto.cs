using System.Collections.Generic;

namespace Frever.AdminService.Core.Services.Localizations;

public class LocalizationDto
{
    public string Key { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Values { get; set; }
}