using Amazon.S3;
using Amazon.S3.Model;
using Common.Infrastructure.Videos;
using Common.Models;
using FluentAssertions;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Features.Feeds.Account;
using Frever.Video.Core.Features.Manipulation;
using Frever.Video.Core.Features.MediaConversion.Client;
using Frever.Video.Core.Features.MediaConversion.StatusUpdating;
using Frever.Video.Core.Features.Shared;
using Frever.Video.Core.Features.Uploading;
using Frever.Video.Core.Features.Uploading.Models;
using Frever.Video.Core.IntegrationTest.Data;
using Frever.Video.Core.IntegrationTest.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Video.Core.IntegrationTest.Features;

public class VideoPublishingIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task PublishNonLevelVideo_HappyPath()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddVideoIntegrationTests(testOut);

        var s3 = new Mock<IAmazonS3>();
        services.AddSingleton(s3.Object);
        s3.Setup(s => s.GetObjectAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
          .ReturnsAsync(new GetObjectResponse {ResponseStream = new MemoryStream()});
        s3.Setup(s => s.GetPreSignedURL(It.IsAny<GetPreSignedUrlRequest>())).Returns("s3:signed");
        
        var mediaConvertClient = new Mock<IMediaConvertServiceClient>();
        services.AddSingleton(mediaConvertClient.Object);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var aiContent = await dataEnv.WithAiContent(user);

        var testService = provider.GetRequiredService<IVideoUploadService>();

        // Act

        var upload = await testService.CreateVideoUpload();
        var param = new CompleteNonLevelVideoUploadingRequest
                    {
                        Access = VideoAccess.Private,
                        Description = "test video",
                        Size = 100,
                        AllowComment = true,
                        AllowRemix = true,
                        DurationSec = 10,
                        VideoOrientation = VideoOrientation.Portrait,
                        PublishTypeId = KnownVideoTypes.StandardId,
                        AiContentId = aiContent.Id
                    };
        var result = await testService.CompleteNonLevelVideoUploading(upload.UploadId, param);

        var dbVideo = await dataEnv.Db.Video.SingleOrDefaultAsync(s => s.Id == result.Id);
        result.Should().BeEquivalentTo(dbVideo);

        result.Access.Should().Be(VideoAccess.Private);
        result.Description.Should().Be(param.Description);
        result.Size.Should().Be(param.Size);
        result.AllowComment.Should().Be(param.AllowComment);
        result.AllowRemix.Should().Be(param.AllowRemix);
        result.Duration.Should().Be(param.DurationSec);
        result.LevelId.Should().BeNull();
        result.PublishTypeId.Should().Be(param.PublishTypeId);
        result.AiContentId.Should().Be(aiContent.Id);

        mediaConvertClient.Verify(
            c => c.CreateVideoConversionJob(
                result.Id,
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
                It.Is<string>(s => !string.IsNullOrWhiteSpace(s)),
                false
            )
        );

        var videoStatusConversion = provider.GetRequiredService<IVideoConversionStatusUpdateService>();
        await videoStatusConversion.HandleVideoConversionCompletion(result.Id, VideoConversionType.Thumbnail);
        await videoStatusConversion.HandleVideoConversionCompletion(result.Id, VideoConversionType.Video);

        var accountVideoFeed = provider.GetRequiredService<IAccountVideoFeed>();

        var myVideos = await accountVideoFeed.GetMyVideos(null, 100, 0);
        myVideos.Should().HaveCount(1);

        var videoManipulation = provider.GetRequiredService<IVideoManipulationService>();
        await videoManipulation.DeleteVideo(result.Id);

        var myVideos2 = await accountVideoFeed.GetMyVideos(null, 100, 0);
        myVideos2.Should().HaveCount(0);
    }
}