using System;

namespace Frever.Video.Contract;

public class VideoUploadInfo(string uploadId, string uploadUrl)
{
    public string UploadUrl { get; } = uploadUrl ?? throw new ArgumentNullException(nameof(uploadUrl));
    public string UploadId { get; } = uploadId ?? throw new ArgumentNullException(nameof(uploadId));
}