using Common.Models.Files;

namespace Frever.AdminService.Core.Services.CreatePage;

public class CreatePageRowResponse
{
    public long Id { get; set; }
    public string Title { get; set; }
    public int SortOrder { get; set; }
    public string TestGroup { get; set; }
    public string ContentType { get; set; }
    public ContentShortResponse[] Content { get; set; }
    public string ContentQuery { get; set; }
    public bool IsEnabled { get; set; }
}

public class ContentShortResponse : IFileMetadataOwner
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string ThumbnailUrl { get; set; }
    public FileMetadata[] Files { get; set; }
}