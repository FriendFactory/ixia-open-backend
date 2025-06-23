using System;

namespace Common.Infrastructure.CloudFront;

public class CloudFrontConfiguration
{
    public string CloudFrontHost { get; set; }

    public string CloudFrontCertPrivateKey { get; set; }

    public string CloudFrontCertKeyPairId { get; set; }

    public int CloudFrontSignedCookieLifetimeMinutes { get; set; }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CloudFrontHost))
            ThrowConfigError(nameof(CloudFrontHost));
        if (string.IsNullOrWhiteSpace(CloudFrontCertPrivateKey))
            ThrowConfigError(nameof(CloudFrontCertPrivateKey));
        if (string.IsNullOrWhiteSpace(CloudFrontCertKeyPairId))
            ThrowConfigError(nameof(CloudFrontCertKeyPairId));
        if (CloudFrontSignedCookieLifetimeMinutes == 0)
            CloudFrontSignedCookieLifetimeMinutes = 60 * 10; // Ten hours
        return;

        void ThrowConfigError(string name)
        {
            throw new InvalidOperationException($"Missing value for {name} video server option");
        }
    }
}