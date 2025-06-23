using System;
using System.Threading.Tasks;
using Common.Infrastructure.Aws.Crypto;
using Common.Infrastructure.Utils;
using Microsoft.Extensions.Logging;

#pragma warning disable CS1998

namespace AssetServer.Services;

internal sealed class AwsCloudFrontService : ICloudFrontService
{
    private readonly ILogger _logger;
    private readonly AssetServiceOptions _options;

    public AwsCloudFrontService(AssetServiceOptions options, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = loggerFactory.CreateLogger("AwsCloudFrontService");
    }

    public string CreateCdnUrl(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(filePath));

        _logger.LogDebug("Create signed URL for {FilePath}", filePath);

        var url = UriUtils.CombineUri(_options.AssetCdnHost, filePath);

        _logger.LogDebug("Full path is {Url}", url);

        return url;
    }

    public async Task<string> SignUrl(string cdnUrl)
    {
        if (string.IsNullOrWhiteSpace(cdnUrl))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(cdnUrl));

        _logger.LogDebug(
            "Signing URL with options: CRT Pair ID={CloudFrontCertificateKeyPairId}, expired at {AddMinutes}",
            _options.CloudFrontCertificateKeyPairId,
            DateTime.Now.AddMinutes(_options.AssetUrlLifetimeMinutes)
        );

        return FreverAmazonCloudFrontSigner.SignUrlCanned(
            cdnUrl,
            _options.CloudFrontCertificateKeyPairId,
            DateTime.Now.AddMinutes(_options.AssetUrlLifetimeMinutes)
        );
    }
}