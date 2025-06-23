using FluentValidation;

#pragma warning disable CS8618

namespace Frever.Video.Core.Features;

public class VideoServerOptions
{
    public string IngestVideoS3BucketName { get; set; }

    public string DestinationVideoS3BucketName { get; set; }

    public string CloudFrontHost { get; set; }

    public string ConvertJobTemplateName { get; set; }

    public string VideoThumbnailJobTemplateName { get; set; }

    public string TranscodingJobTemplateName { get; set; }

    public string CloudFrontCertPrivateKey { get; set; }

    public string CloudFrontCertKeyPairId { get; set; }

    public int CloudFrontSignedCookieLifetimeMinutes { get; set; } = 60 * 10; // Ten hours

    public string ConvertJobRoleArn { get; set; }

    public string VideoPlayerPageUrl { get; set; }

    public string VideoReportNotificationEmail { get; set; }

    public int TrendingVideoListLength { get; set; } = 150;

    public string VideoConversionSqsQueue { get; set; }

    public string ConversionJobSqsQueue { get; set; }

    public string MediaConverterQueue { get; set; }

    public string ExtractAudioQueue { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<VideoServerOptions>();

        validator.RuleFor(e => e.IngestVideoS3BucketName).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.DestinationVideoS3BucketName).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.CloudFrontHost).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.CloudFrontCertPrivateKey).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.CloudFrontCertKeyPairId).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.ConvertJobTemplateName).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.VideoThumbnailJobTemplateName).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.TranscodingJobTemplateName).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.ConvertJobRoleArn).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.CloudFrontSignedCookieLifetimeMinutes).GreaterThan(0);
        validator.RuleFor(e => e.VideoPlayerPageUrl).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.VideoReportNotificationEmail).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.VideoConversionSqsQueue).NotEmpty().MinimumLength(1);
        validator.RuleFor(e => e.ConversionJobSqsQueue).NotEmpty().MinimumLength(1);

        validator.ValidateAndThrow(this);
    }
}