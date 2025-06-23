namespace Frever.Video.Core.Features.MediaConversion.Mp3Extraction;

public class TranscodeResult
{
    public bool Ok { get; set; }

    public string ErrorCode { get; set; }

    public string ErrorDescription { get; set; }

    public string ConvertedFileUrl { get; set; }

    public string MediaIdentificationResultRaw { get; set; }
}