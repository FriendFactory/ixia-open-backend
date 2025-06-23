using Common.Infrastructure;
using FluentAssertions;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.Client.Shared.Files;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Frever.Client.Core.IntegrationTest.Features.AI;

public partial class AiContentTest
{
    [Fact]
    public async Task DeleteContent_ShouldWork()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var service = provider.GetRequiredService<IAiGeneratedContentService>();

        var image1 = await CreateAiImage(dataEnv, user, service);
        var video2 = await CreateAiVideo(dataEnv, user, service);
        var video3 = await CreateAiVideo(dataEnv, user, service);
        var image4 = await CreateAiVideo(dataEnv, user, service);

        var list = await service.GetDrafts(null, 0, 50);
        list.Should().HaveCount(4);

        var existing = await service.GetById(video2.Id);
        existing.Should().NotBeNull();

        await service.Delete(video2.Id);

        var deleted = await service.GetById(video2.Id);
        deleted.Should().BeNull();

        var list2 = await service.GetDrafts(null, 0, 100);
        list2.Should().HaveCount(3);
        list2.Select(l => l.Id).ToArray().Should().BeEquivalentTo(new[] {image1.Id, video3.Id, image4.Id});
    }

    [Fact]
    public async Task DeletePublishedContent_ShouldWork()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        var externalFileDownloaded = new Mock<IExternalFileDownloader>();
        services.AddSingleton(externalFileDownloaded.Object);


        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var service = provider.GetRequiredService<IAiGeneratedContentService>();

        var image1 = await CreateAiImage(dataEnv, user, service);
        var video2 = await CreateAiVideo(dataEnv, user, service);
        var video3 = await CreateAiVideo(dataEnv, user, service);
        var image4 = await CreateAiImage(dataEnv, user, service);

        foreach (var item in new[] {image1, video2, video3, image4})
            await service.Publish(item.Id);

        var list = await service.GetDrafts(null, 0, 50);
        list.Should().HaveCount(4); // All content published, no drafts

        var existing = await service.GetById(video2.Id);
        existing.Should().NotBeNull();

        var feed = await service.GetFeed(user.MainGroupId, null, 0, 50);
        feed.Should().HaveCount(4); // All content published, no drafts

        await service.Delete(video2.Id);
        var empty = await service.GetById(video2.Id);
        empty.Should().BeNull();

        await service.Delete(image1.Id);

        await FluentActions.Awaiting(async () => await service.Delete(video2.Id)).Should().ThrowAsync<AppErrorWithStatusCodeException>();
        await service.Delete(video3.Id);
        await service.Delete(image4.Id);

        var noDrafts = await service.GetDrafts(null, 0, 50);
        noDrafts.Should().HaveCount(0);

        var noFeed = await service.GetFeed(user.MainGroupId, null, 0, 50);
        noFeed.Should().HaveCount(4, "deleting drafts should not affect the feed");
    }

    [Fact]
    public async Task DeleteContent_ShouldNotAllowToDeleteOthersContent()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstUser = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        firstScope.ServiceProvider.SetCurrentUser(firstUser);

        var secondUser = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        secondScope.ServiceProvider.SetCurrentUser(secondUser);

        var firstService = firstScope.ServiceProvider.GetRequiredService<IAiGeneratedContentService>();

        _ = await CreateAiImage(dataEnv, firstUser, firstService);
        var video2 = await CreateAiVideo(dataEnv, firstUser, firstService);
        _ = await CreateAiVideo(dataEnv, firstUser, firstService);
        _ = await CreateAiVideo(dataEnv, firstUser, firstService);

        var secondService = secondScope.ServiceProvider.GetRequiredService<IAiGeneratedContentService>();
        await FluentActions.Awaiting(async () => await secondService.Delete(video2.Id)).Should().ThrowAsync<Exception>();
    }
}