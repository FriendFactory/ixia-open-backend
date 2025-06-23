namespace Frever.Video.Core.Features.Uploading.Models;

public class CompleteNonLevelVideoUploadingRequest : VideoUploadingRequestBase
{
    public string Format { get; set; }

    public long? AiContentId { get; set; }
}