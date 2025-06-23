using FluentAssertions;
using FluentValidation;
using Frever.Client.Core.Features.AI.Moderation.External.OpenAi;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.Client.Shared.Files;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Frever.Client.Core.IntegrationTest.Features.AI;

public partial class AiContentTest
{
    [Fact]
    public async Task Publish_ShouldWork()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        var externalFileDownloaded = new Mock<IExternalFileDownloader>();
        services.AddSingleton(externalFileDownloaded.Object);

        await using var provider = services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        using var firstScope = provider.CreateScope();
        var firstUser = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        firstScope.ServiceProvider.SetCurrentUser(firstUser);
        var firstService = firstScope.ServiceProvider.GetRequiredService<IAiGeneratedContentService>();

        var image1 = await CreateAiImage(dataEnv, firstUser, firstService);
        var publishedResult1 = await firstService.Publish(image1.Id);
        publishedResult1.Should().NotBeNull();

        var published1 = await firstService.GetById(publishedResult1.Id);
        published1.Should().NotBeNull();
    }

    [Fact]
    public async Task Publish_ShouldFailIfModerationFailed()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        var externalFileDownloaded = new Mock<IExternalFileDownloader>();
        services.AddSingleton(externalFileDownloaded.Object);

        await using var provider = services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var openAiClientMock = provider.GetRequiredService<Mock<IOpenAiClient>>();
        openAiClientMock.Setup(s => s.Moderate(It.IsAny<TextInput>(), It.IsAny<ImageUrlInput>()))
                        .ReturnsAsync(
                             new OpenAiModerationResponse
                             {
                                 Id = $"test-moderation-{Guid.NewGuid():N}",
                                 Model = "mock-model",
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


        using var firstScope = provider.CreateScope();
        var firstUser = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        firstScope.ServiceProvider.SetCurrentUser(firstUser);
        var firstService = firstScope.ServiceProvider.GetRequiredService<IAiGeneratedContentService>();

        var image1 = await CreateAiImage(dataEnv, firstUser, firstService);

        await FluentActions.Awaiting(async () => _ = await firstService.Publish(image1.Id))
                           .Should()
                           .ThrowAsync<AiContentModerationException>();

        var dbImage1 = await dataEnv.Db.AiGeneratedImage.AsNoTracking().FirstOrDefaultAsync(i => i.Id == image1.Image.Id);
        dbImage1.Should().NotBeNull();
        dbImage1.IsModerationPassed.Should().BeFalse();
        dbImage1.ModerationResult.Should().NotBeNull();
        dbImage1.ModerationResult.IsPassed.Should().BeFalse();
        dbImage1.ModerationResult.Items.Should().HaveCount(2);

        var published = await dataEnv.Db.AiGeneratedContent.FirstOrDefaultAsync(c => c.DraftAiContentId == image1.Id);
        published.Should().BeNull();
    }
}