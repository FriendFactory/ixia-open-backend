using System.Net;
using AuthServer.Permissions;
using Common.Infrastructure.CloudFront;
using Frever.Client.Shared.Social;
using Frever.Common.IntegrationTesting;
using Frever.Video.Core.Features;
using Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace Frever.Video.Core.IntegrationTest.Utils;

public static class TestServiceConfiguration
{
    public static void AddVideoIntegrationTests(this IServiceCollection services, ITestOutputHelper testOut)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(testOut);


        var configuration = IntegrationTestServiceConfiguration.GetConfiguration();

        var videoServerOptions = new VideoServerOptions();
        configuration.Bind(videoServerOptions);

        services.AddSocialSharedService();
        services.AddCloudFrontConfiguration(configuration);
        services.AddVideoServices(configuration);

        AddCurrentUserLocationProvider(services);

        // This should be last to overwrite IReadDb/IWriteDb registration from AddXXX business methods.
        services.AddIntegrationTests(testOut);
    }

    private static void AddCurrentUserLocationProvider(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var mock = new Mock<IIpAddressProvider>();
        mock.Setup(s => s.GetIpAddressOfConnectedClient()).Returns(new IPAddress([146, 70, 162, 114]));

        services.AddSingleton(mock);
        services.AddSingleton(mock.Object);
    }
}