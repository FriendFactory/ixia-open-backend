namespace Frever.Client.Core.Features.MediaFingerprinting;

public class MediaFingerprintingResult
{
    public bool Ok { get; set; }

    public bool ContainsCopyrightedContent { get; set; }

    public string ErrorCode { get; set; }

    public string ErrorMessage { get; set; }

    public string Response { get; set; }
}