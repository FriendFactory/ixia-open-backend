using System.Collections.Generic;

namespace Frever.Video.Core.Features.CreatePage;

public class CreatePageContentResponse
{
    public List<CreatePageRowShortResponse> Rows { get; set; } = [];
}

public class CreatePageRowShortResponse
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string ContentType { get; set; }
}