using System.Collections.Generic;

namespace Frever.Client.Core.Features.CommercialMusic;

public class SignUrlRequest
{
    public SortedDictionary<string, string> QueryParameters { get; set; }
    public string BaseUrl { get; set; }
    public string HttpMethod { get; set; }
}