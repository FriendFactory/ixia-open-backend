using System.Collections.Generic;

namespace Frever.AdminService.Core.Services.MusicProvider;

public class MusicProviderRequest
{
    public string BaseUrl { get; set; }

    public string Body { get; set; }

    public string HttpMethod { get; set; }

    public SortedDictionary<string, string> QueryParameters { get; set; }
}