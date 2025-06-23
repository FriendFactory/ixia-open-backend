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
    public async Task Publish_ShouldStoreModerationInfoInContent()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        var externalFileDownloaded = new Mock<IExternalFileDownloader>();
        services.AddSingleton(externalFileDownloaded.Object);

        await using var provider = services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var openAiClientMock = provider.GetRequiredService<Mock<IOpenAiClient>>();
        var openAiModerationResponse = new OpenAiModerationResponse
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
                                       };

        openAiClientMock.Setup(s => s.Moderate(It.IsAny<TextInput>(), It.IsAny<ImageUrlInput>())).ReturnsAsync(openAiModerationResponse);


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

        var mainFileResult = dbImage1.ModerationResult.Items.FirstOrDefault(i => i.ContentId == "main");

        mainFileResult.Response.Should().BeEquivalentTo(openAiModerationResponse);
        mainFileResult.IsPassed.Should().BeFalse();
        mainFileResult.Value.Should().Be(dbImage1.Files.First(f => f.Type == "main").Path);
        mainFileResult.ContentId.Should().Be("main");
        mainFileResult.MediaType.Should().Be("image");

        var published = await dataEnv.Db.AiGeneratedContent.FirstOrDefaultAsync(c => c.DraftAiContentId == image1.Id);
        published.Should().BeNull();
    }

    [Fact]
    public async Task Publish_ShouldStoreModerationInfoInPublishedContent()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        var externalFileDownloaded = new Mock<IExternalFileDownloader>();
        services.AddSingleton(externalFileDownloaded.Object);

        await using var provider = services.BuildServiceProvider();
        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var openAiClientMock = provider.GetRequiredService<Mock<IOpenAiClient>>();
        var openAiModerationResponse = new OpenAiModerationResponse
                                       {
                                           Id = $"test-moderation-{Guid.NewGuid():N}",
                                           Model = "mock-model",
                                           Results =
                                           [
                                               new OpenAiModerationResult
                                               {
                                                   Flagged = false,
                                                   Categories = new Dictionary<string, bool> {{"test", true}},
                                                   CategoryScores = new Dictionary<string, decimal> {{"test", 0.0001m}},
                                                   CategoryAppliedInputTypes = []
                                               }
                                           ]
                                       };

        openAiClientMock.Setup(s => s.Moderate(It.IsAny<TextInput>(), It.IsAny<ImageUrlInput>())).ReturnsAsync(openAiModerationResponse);


        using var firstScope = provider.CreateScope();
        var firstUser = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        firstScope.ServiceProvider.SetCurrentUser(firstUser);
        var firstService = firstScope.ServiceProvider.GetRequiredService<IAiGeneratedContentService>();

        var image1 = await CreateAiImage(dataEnv, firstUser, firstService);

        var publishedItem = await firstService.Publish(image1.Id);
        publishedItem.Should().NotBeNull();


        var dbImage1 = await dataEnv.Db.AiGeneratedImage.AsNoTracking().FirstOrDefaultAsync(i => i.Id == image1.Image.Id);
        dbImage1.Should().NotBeNull();
        dbImage1.IsModerationPassed.Should().BeTrue();
        dbImage1.ModerationResult.Should().NotBeNull();
        dbImage1.ModerationResult.IsPassed.Should().BeTrue();
        dbImage1.ModerationResult.Items.Should().HaveCount(2);

        var mainFileResult = dbImage1.ModerationResult.Items.FirstOrDefault(i => i.ContentId == "main");
        mainFileResult.Response.Should().BeEquivalentTo(openAiModerationResponse);
        mainFileResult.IsPassed.Should().BeTrue();
        mainFileResult.Value.Should().Be(dbImage1.Files.First(f => f.Type == "main").Path);
        mainFileResult.ContentId.Should().Be("main");
        mainFileResult.MediaType.Should().Be("image");


        var promptResult = dbImage1.ModerationResult.Items.FirstOrDefault(i => i.ContentId == "prompt");
        promptResult.Response.Should().BeEquivalentTo(openAiModerationResponse);
        promptResult.IsPassed.Should().BeTrue();
        promptResult.Value.Should().Be(dbImage1.Prompt);
        promptResult.ContentId.Should().Be("prompt");
        promptResult.MediaType.Should().Be("text");


        var dbImage2 = await dataEnv.Db.AiGeneratedImage.AsNoTracking().FirstOrDefaultAsync(i => i.Id == publishedItem.Image.Id);
        dbImage2.Should().NotBeNull();
        dbImage2.IsModerationPassed.Should().BeTrue();
        dbImage2.ModerationResult.Should().NotBeNull();
        dbImage2.ModerationResult.IsPassed.Should().BeTrue();
        dbImage2.ModerationResult.Items.Should().HaveCount(2);

        var publishedMainFileResult = dbImage2.ModerationResult.Items.FirstOrDefault(i => i.ContentId == "main");
        publishedMainFileResult.Should().NotBeNull();
        publishedMainFileResult.Response.Should().BeEquivalentTo(openAiModerationResponse);
        publishedMainFileResult.IsPassed.Should().BeTrue();
        publishedMainFileResult.Value.Should().Be(dbImage2.Files.First(f => f.Type == "main").Path);
        publishedMainFileResult.ContentId.Should().Be("main");
        publishedMainFileResult.MediaType.Should().Be("image");

        var publishedPromptResult = dbImage2.ModerationResult.Items.FirstOrDefault(i => i.ContentId == "prompt");
        publishedPromptResult.Response.Should().BeEquivalentTo(openAiModerationResponse);
        publishedPromptResult.IsPassed.Should().BeTrue();
        publishedPromptResult.Value.Should().Be(dbImage1.Prompt);
        publishedPromptResult.ContentId.Should().Be("prompt");
        publishedPromptResult.MediaType.Should().Be("text");

        var published = await dataEnv.Db.AiGeneratedContent.FirstOrDefaultAsync(c => c.Id == publishedItem.Id);
        published.Should().NotBeNull();
    }
}