using FluentValidation;

namespace Common.Infrastructure.RateLimit;

public class RateLimitThreshold
{
    public bool Enabled { get; set; }
    public string FreverVideoAndAssetDownload { get; set; }
    public string SevenDigitalSongDownload { get; set; }
    public string HardLimitPerUserPerHour { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<RateLimitThreshold>();
        validator.RuleFor(a => a.FreverVideoAndAssetDownload).NotEmpty().MinimumLength(1);
        validator.RuleFor(a => a.SevenDigitalSongDownload).NotEmpty().MinimumLength(1);
        validator.RuleFor(a => a.HardLimitPerUserPerHour).NotEmpty().MinimumLength(1);

        validator.ValidateAndThrow(this);
    }

    public (int, string) ToFreverVideoAndAssetDownloadLimitAndPeriod()
    {
        var assetDownloadThreshold = double.Parse(FreverVideoAndAssetDownload) > 1 ? int.Parse(FreverVideoAndAssetDownload) : 1;
        var assetDownloadPeriod =
            (int) (double.Parse(FreverVideoAndAssetDownload) > 1 ? 1 : 1 / double.Parse(FreverVideoAndAssetDownload)) + "s";
        return (assetDownloadThreshold, assetDownloadPeriod);
    }

    public (int, string) ToSevenDigitalSongDownloadLimitAndPeriod()
    {
        var assetDownloadThreshold = double.Parse(SevenDigitalSongDownload) > 1 ? int.Parse(SevenDigitalSongDownload) : 1;
        var assetDownloadPeriod = (int) (double.Parse(SevenDigitalSongDownload) > 1 ? 1 : 1 / double.Parse(SevenDigitalSongDownload)) + "s";
        return (assetDownloadThreshold, assetDownloadPeriod);
    }
}