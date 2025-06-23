using System;
using System.Threading.Tasks;
using Amazon.S3;
using FluentValidation;

namespace Frever.Video.Core.Features.PersonalFeed.Tracing;

public class AwsS3PersonalFeedTraceFactory : IPersonalFeedTracerFactory
{
    private readonly AwsS3PersonalFeedTracerOptions _options;
    private readonly IAmazonS3 _s3;

    public AwsS3PersonalFeedTraceFactory(IAmazonS3 s3, AwsS3PersonalFeedTracerOptions options)
    {
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        options.Validate();
    }

    public Task<IPersonalFeedTracer> StartMixingEngineTracing(long groupId)
    {
        return Task.FromResult<IPersonalFeedTracer>(new AwsS3PersonalFeedTracer(groupId, _options.Bucket, _s3));
    }

    public Task<IMLFeedTracer> StartMLFeedTracing(long groupId)
    {
        return Task.FromResult<IMLFeedTracer>(new AwsS3MLFeedTracer(groupId, _options.Bucket, _s3));
    }
}

public class AwsS3PersonalFeedTracerOptions
{
    public string Bucket { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<AwsS3PersonalFeedTracerOptions>();

        validator.RuleFor(e => e.Bucket).NotEmpty().MinimumLength(1);

        validator.ValidateAndThrow(this);
    }
}