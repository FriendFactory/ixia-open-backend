using System;
using System.Threading.Tasks;
using Amazon.S3;
using AssetServer.Shared.AssetCopying;
using Common.Infrastructure.Aws;
using Common.Infrastructure.CloudFront;
using FluentAssertions;
using Frever.Client.Core.Features.AI.Moderation;
using Frever.Client.Core.Features.AI.Moderation.External.OpenAi;
using Frever.Client.Shared.Files;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Client.Core.Test.Features.AI.Moderation;

public class OpenAiContentModerationServiceTest
{
    private readonly IAiContentModerationService service;
    private readonly Mock<IOpenAiClient> openAiClientMock = new();

    public OpenAiContentModerationServiceTest(ITestOutputHelper testOut)
    {
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();

        var services = new ServiceCollection();
        services.AddUnitTestServices(testOut);
        services.AddAiContentModeration();
        services.AddEntityFiles();
        services.AddSingleton(openAiClientMock.Object);

        services.AddCloudFrontConfiguration(configuration);
        services.AddConfiguredAWSOptions(configuration);
        services.AddAWSService<IAmazonS3>();

        var assetCopyingOptions = new AssetCopyingOptions();
        configuration.Bind("AssetCopying", assetCopyingOptions);
        services.AddAssetCopying(assetCopyingOptions);

        var provider = services.BuildServiceProvider();

        service = provider.GetRequiredService<IAiContentModerationService>();
    }

    [Fact]
    public async Task ModerateText_ShouldWork()
    {
        var customWeights = new Dictionary<string, decimal>();
        openAiClientMock.Setup(s => s.Moderate(It.IsAny<TextInput>(), null))
                        .ReturnsAsync(
                             new OpenAiModerationResponse
                             {
                                 Id = "123",
                                 Model = "test-model",
                                 Results =
                                 [
                                     new OpenAiModerationResult
                                     {
                                         Flagged = false,
                                         Categories = [],
                                         CategoryScores = [],
                                         CategoryAppliedInputTypes = []
                                     }
                                 ]
                             }
                         );


        var result = await service.ModerateText("Potentially harmful text", "shortPrompt", customWeights);

        result.Should().NotBeNull();
        result.IsPassed.Should().BeTrue();
        result.ContentId.Should().Be("shortPrompt");
        result.Value.Should().Be("Potentially harmful text");
        result.MediaType.Should().Be("text");
        // result.CustomCategoryWeights.Should().BeSameAs(customWeights);
    }

    [Fact]
    public async Task ModerateImage_ShouldWork()
    {
        var customWeights = new Dictionary<string, decimal>();
        openAiClientMock.Setup(s => s.Moderate(null, It.IsAny<ImageUrlInput>()))
                        .ReturnsAsync(
                             new OpenAiModerationResponse
                             {
                                 Id = "123",
                                 Model = "test-model",
                                 Results =
                                 [
                                     new OpenAiModerationResult
                                     {
                                         Flagged = false,
                                         Categories = [],
                                         CategoryScores = [],
                                         CategoryAppliedInputTypes = []
                                     }
                                 ]
                             }
                         );


        var result = await service.ModerateImage("content/image/123/content.jpg", "main", customWeights);

        result.Should().NotBeNull();
        result.IsPassed.Should().BeTrue();
        result.ContentId.Should().Be("main");
        result.Value.Should().Be("content/image/123/content.jpg");
        result.MediaType.Should().Be("image");
        // result.CustomCategoryWeights.Should().BeSameAs(customWeights);
    }

    [Fact]
    public async Task ModerateText_ShouldFailIfOpenAiResponseFlagged()
    {
        var customWeights = new Dictionary<string, decimal>();
        openAiClientMock.Setup(s => s.Moderate(It.IsAny<TextInput>(), null))
                        .ReturnsAsync(
                             new OpenAiModerationResponse
                             {
                                 Id = "123",
                                 Model = "test-model",
                                 Results =
                                 [
                                     new OpenAiModerationResult
                                     {
                                         Flagged = true,
                                         Categories = [],
                                         CategoryScores = [],
                                         CategoryAppliedInputTypes = []
                                     }
                                 ]
                             }
                         );


        var result = await service.ModerateText("Potentially harmful text", "shortPrompt", customWeights);

        result.Should().NotBeNull();
        result.IsPassed.Should().BeFalse();
        result.ContentId.Should().Be("shortPrompt");
        result.Value.Should().Be("Potentially harmful text");
        result.MediaType.Should().Be("text");
        // result.CustomCategoryWeights.Should().BeSameAs(customWeights);
    }

    [Fact]
    public async Task ModerateText_ShouldFailIfOpenAiResponseOkButCustomWeightsExceeded()
    {
        var customWeights = new Dictionary<string, decimal> {{"harassement", 0.2M}};
        openAiClientMock.Setup(s => s.Moderate(It.IsAny<TextInput>(), null))
                        .ReturnsAsync(
                             new OpenAiModerationResponse
                             {
                                 Id = "123",
                                 Model = "test-model",
                                 Results =
                                 [
                                     new OpenAiModerationResult
                                     {
                                         Flagged = false,
                                         Categories = [],
                                         CategoryScores = new Dictionary<string, decimal> {{"harassement", 0.3M}},
                                         CategoryAppliedInputTypes = []
                                     }
                                 ]
                             }
                         );


        var result = await service.ModerateText("Potentially harmful text", "shortPrompt", customWeights);

        result.Should().NotBeNull();
        result.IsPassed.Should().BeFalse();
        result.ContentId.Should().Be("shortPrompt");
        result.Value.Should().Be("Potentially harmful text");
        result.MediaType.Should().Be("text");
        // result.CustomCategoryWeights.Should().BeSameAs(customWeights);
    }

    [Fact]
    public async Task ModerateText_ShouldFailIfOpenAiResponseOkButCustomWeightsNotExceeded()
    {
        var customWeights = new Dictionary<string, decimal> {{"harassement", 0.5M}};
        openAiClientMock.Setup(s => s.Moderate(It.IsAny<TextInput>(), null))
                        .ReturnsAsync(
                             new OpenAiModerationResponse
                             {
                                 Id = "123",
                                 Model = "test-model",
                                 Results =
                                 [
                                     new OpenAiModerationResult
                                     {
                                         Flagged = false,
                                         Categories = [],
                                         CategoryScores = new Dictionary<string, decimal> {{"harassement", 0.3M}},
                                         CategoryAppliedInputTypes = []
                                     }
                                 ]
                             }
                         );


        var result = await service.ModerateText("Potentially harmful text", "shortPrompt", customWeights);

        result.Should().NotBeNull();
        result.IsPassed.Should().BeTrue();
        result.ContentId.Should().Be("shortPrompt");
        result.Value.Should().Be("Potentially harmful text");
        result.MediaType.Should().Be("text");
        // result.CustomCategoryWeights.Should().BeSameAs(customWeights);
    }
}