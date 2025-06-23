using System.Collections.Generic;
using Frever.Client.Shared.Files;

namespace Frever.Video.Core.Features.CreatePage;

public class CreatePageContent
{
    public List<CreatePageRow> Rows { get; set; } = [];
}

public class CreatePageRow
{
    public long Id { get; set; }
    public string Title { get; set; }
    public int SortOrder { get; set; }
    public string TestGroup { get; set; }
    public string Type { get; set; }
    public long[] ContentIds { get; set; } = [];
}

public class AiGeneratedImageFileConfig : DefaultFileMetadataConfiguration<Frever.Shared.MainDb.Entities.AiGeneratedImage>
{
    public AiGeneratedImageFileConfig()
    {
        AddMainFile("jpeg");
        AddThumbnail(128, "jpeg");
    }
}