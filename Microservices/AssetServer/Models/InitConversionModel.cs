namespace AssetServer.Services;

public class InitConversionModel
{
    public string UploadUrl { get; set; }
    public string UploadId { get; set; }
    public string ConvertedFileUrl { get; set; }
    public string CheckFileConvertedUrl { get; set; }
    public string OriginalFileExtension { get; set; }
    public string TargetFileExtension { get; set; }
}