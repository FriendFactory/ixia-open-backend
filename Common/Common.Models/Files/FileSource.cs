namespace Common.Models.Files;

public sealed class FileSource
{
    public string UploadId { get; set; }
    public AssetFileSourceInfo CopyFrom { get; set; }

    public void Deconstruct(out string uploadId, out AssetFileSourceInfo copyFrom)
    {
        uploadId = UploadId;
        copyFrom = CopyFrom;
    }
}