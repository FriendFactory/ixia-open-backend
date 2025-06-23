using AssetStoragePathProviding;
using Common.Infrastructure.Aws.Crypto;
using Frever.Common.Testing;
using Frever.Video.Core.Features.AssetUrlGeneration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Frever.Video.Core.Test.Utils;

public static class TestServiceConfiguration
{
    public static void AddTestVideoServices(this IServiceCollection services, ITestOutputHelper testOut)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(testOut);

        services.AddUnitTestServices(testOut);

        AddVideoAssetUrlGenerator(services);
        AddVideoNamingHelper(services);
    }

    private static void AddVideoNamingHelper(IServiceCollection services)
    {
        var videoNamingHelper = new VideoNamingHelper(
            new VideoNamingHelperOptions
            {
                CloudFrontHost = "http://mock.frever-content.com",
                DestinationVideoBucket = "frever-mock",
                IngestVideoBucket = "frever-mock-video-in"
            }
        );
        services.AddSingleton(videoNamingHelper);
    }

    private static void AddVideoAssetUrlGenerator(IServiceCollection services)
    {
        var mock = new Mock<IVideoAssetUrlGenerator>();

        mock.Setup(s => s.CreateSignedCookie(It.IsAny<IVideoNameSource>()))
            .Returns(
                 Task.FromResult(
                     new FreverAmazonCloudFrontCookiesForCustomPolicy
                     {
                         Policy = new KeyValuePair<string, string>("Policy", "Policy"),
                         KeyPairId = new KeyValuePair<string, string>("KeyPairId", "KeyPairId"),
                         Signature = new KeyValuePair<string, string>("Signature", "Signature")
                     }
                 )
             );

        mock.Setup(s => s.CreateSignedUrl(It.IsAny<string>())).Returns((string url) => url + "&itsvalidiswear");
        mock.Setup(s => s.GetThumbnailUrl(It.IsAny<IVideoNameSource>())).Returns((IVideoNameSource video) => $"{video.Id}-thumbnail");

        services.AddSingleton(mock);
        services.AddSingleton(mock.Object);
    }

    private static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder().AddEnvironmentVariables().Build();
    }
}