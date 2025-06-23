using Common.Models.Files;
using FluentAssertions;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.Client.Core.IntegrationTest.Features.AI.Data;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.Client.Shared.Files;
using Frever.ClientService.Contract.Ai;
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
    public async Task GetDrafts_ShouldWork()
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

        var ordered = new[] {image4, video3, video2, image1};
        for (var i = 0; i < list.Length; i++)
        {
            var expected = ordered[i];
            var actual = list[i];

            actual.Id.Should().Be(expected.Id);
            actual.Type.Should().Be(expected.Type);
            actual.Group.Should().NotBeNull();
            actual.Group.Id.Should().Be(expected.GroupId);

            CompareFiles(expected.Image as IFileMetadataOwner ?? expected.Video, actual);
        }
    }

    [Fact]
    public async Task GetPublishedList_ShouldWork()
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

        using var secondScope = provider.CreateScope();
        var secondUser = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        secondScope.ServiceProvider.SetCurrentUser(secondUser);
        var secondService = secondScope.ServiceProvider.GetRequiredService<IAiGeneratedContentService>();

        var image1 = await CreateAiImage(dataEnv, firstUser, firstService);
        var video2 = await CreateAiVideo(dataEnv, firstUser, firstService);
        var video3 = await CreateAiVideo(dataEnv, firstUser, firstService);
        var image4 = await CreateAiVideo(dataEnv, firstUser, firstService);

        var secondImage1 = await CreateAiImage(dataEnv, secondUser, secondService);
        var secondVideo2 = await CreateAiVideo(dataEnv, secondUser, secondService);
        var secondVideo3 = await CreateAiVideo(dataEnv, secondUser, secondService);
        var secondImage4 = await CreateAiVideo(dataEnv, secondUser, secondService);
        foreach (var v in new[] {secondImage1, secondImage4})
            await secondService.Publish(v.Id);

        var secondFeedOfFirstUser = await secondService.GetFeed(firstUser.MainGroupId, null, 0, 100);
        secondFeedOfFirstUser.Should().HaveCount(0);

        var firstUserDrafts = await firstService.GetDrafts(null, 0, 100);
        firstUserDrafts.Should().HaveCount(4);

        await firstService.Publish(video2.Id);

        var secondFeed2 = await secondService.GetFeed(firstUser.MainGroupId, null, 0, 100);
        secondFeed2.Should().HaveCount(1);

        await firstService.Publish(image1.Id);
        var secondFeed3 = await secondService.GetFeed(firstUser.MainGroupId, null, 0, 100);
        secondFeed3.Should().HaveCount(2);

        var secondImageFeed = await secondService.GetFeed(firstUser.MainGroupId, AiGeneratedContentType.Image, 0, 100);
        secondImageFeed.Should().HaveCount(1);

        var secondVideoFeed = await secondService.GetFeed(firstUser.MainGroupId, AiGeneratedContentType.Video, 0, 100);
        secondVideoFeed.Should().HaveCount(1);

        dataEnv.Db.ChangeTracker.Clear();

        var dbPublishedImage1 =
            await dataEnv.Db.AiGeneratedContent.AsNoTracking().FirstOrDefaultAsync(a => a.DraftAiContentId == image1.Id);
        dbPublishedImage1.Status.Should().Be("Published");
    }

    [Fact]
    public async Task GetById_ShouldWork()
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

        using var secondScope = provider.CreateScope();
        var secondUser = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        secondScope.ServiceProvider.SetCurrentUser(secondUser);
        var secondService = secondScope.ServiceProvider.GetRequiredService<IAiGeneratedContentService>();


        var firstImage1 = await CreateAiImage(dataEnv, firstUser, firstService);
        var firstVideo2 = await CreateAiVideo(dataEnv, firstUser, firstService);
        var firstVideo3 = await CreateAiVideo(dataEnv, firstUser, firstService);
        var firstImage4 = await CreateAiVideo(dataEnv, firstUser, firstService);

        var firstDraft = await secondService.GetById(firstImage1.Id);
        firstDraft.Should().BeNull();

        var firstPublished1 = await firstService.Publish(firstImage1.Id);

        var firstPublishedAsSecond = await secondService.GetById(firstPublished1.Id);
        firstPublishedAsSecond.Should().NotBeNull();

        await firstService.Delete(firstPublished1.Id);

        var firstPublishedDeletedAsSecond = await secondService.GetById(firstPublished1.Id);
        firstPublishedDeletedAsSecond.Should().BeNull();

        dataEnv.Db.ChangeTracker.Clear();

        var dbPublishedImage1 = await dataEnv.Db.AiGeneratedContent.AsNoTracking()
                                             .FirstOrDefaultAsync(a => a.Id == firstPublishedAsSecond.Id);
        dbPublishedImage1.Status.Should().Be("Published");
    }

    private async Task<AiGeneratedContentFullInfo> CreateAiImage(DataEnvironment dataEnv, User user, IAiGeneratedContentService service)
    {
        var makeup = await dataEnv.WithAiMakeUp();
        var (_, images) = await dataEnv.WithAiCharacters(user.MainGroupId);
        var aiImage = images[0];

        var input = new AiGeneratedContentInput
                    {
                        Image = new AiGeneratedImageInput
                                {
                                    Seed = 39394,
                                    AiMakeupId = makeup.Id,
                                    Prompt = $"prompt {Guid.NewGuid()}",
                                    ShortPromptSummary = "summary",
                                    Workflow = "wf",
                                    Files =
                                    [
                                        JpegImage("main"),
                                        JpegImage("thumbnail128")
                                    ],
                                    Persons =
                                    [
                                        new AiGeneratedImagePersonInput
                                        {
                                            ParticipantAiCharacterSelfieId = aiImage.Id,
                                            Files =
                                            [
                                                JpegImage("thumbnail128"),
                                                JpegImage("cover"),
                                                JpegImage("main")
                                            ]
                                        }
                                    ],
                                    Sources =
                                    [
                                        new AiGeneratedImageSourceInput
                                        {
                                            Type = AiGeneratedImageSourceType.Background,
                                            Files =
                                            [
                                                JpegImage("main"),
                                                JpegImage("thumbnail128")
                                            ]
                                        },
                                        new AiGeneratedImageSourceInput
                                        {
                                            Type = AiGeneratedImageSourceType.Outfit,
                                            Files =
                                            [
                                                JpegImage("main"),
                                                JpegImage("thumbnail128")
                                            ]
                                        }
                                    ]
                                }
                    };

        var saved = await service.SaveDraft(input);
        return saved;
    }

    private async Task<AiGeneratedContentFullInfo> CreateAiVideo(DataEnvironment dataEnv, User user, IAiGeneratedContentService service)
    {
        var makeup1 = await dataEnv.WithAiMakeUp();
        var makeup2 = await dataEnv.WithAiMakeUp();
        var (_, images1) = await dataEnv.WithAiCharacters(user.MainGroupId);
        var (_, images2) = await dataEnv.WithAiCharacters(user.MainGroupId);
        var aiImage1 = images1[0];
        var aiImage2 = images2[0];

        var imageForClip1 = new AiGeneratedImageInput
                            {
                                Prompt = "test prompt",
                                Seed = 39394,
                                AiMakeupId = makeup1.Id,
                                ShortPromptSummary = "summary",
                                Workflow = "image_wf1",
                                Files =
                                [
                                    JpegImage("main"),
                                    JpegImage("thumbnail128")
                                ],
                                Persons =
                                [
                                    new AiGeneratedImagePersonInput
                                    {
                                        ParticipantAiCharacterSelfieId = aiImage1.Id,
                                        Files =
                                        [
                                            JpegImage("thumbnail128"),
                                            JpegImage("cover"),
                                            JpegImage("main")
                                        ]
                                    }
                                ],
                                Sources =
                                [
                                    new AiGeneratedImageSourceInput
                                    {
                                        Type = AiGeneratedImageSourceType.Background,
                                        Files =
                                        [
                                            JpegImage("main"),
                                            JpegImage("thumbnail128")
                                        ]
                                    },
                                    new AiGeneratedImageSourceInput
                                    {
                                        Type = AiGeneratedImageSourceType.Outfit,
                                        Files =
                                        [
                                            JpegImage("main"),
                                            JpegImage("thumbnail128")
                                        ]
                                    }
                                ]
                            };

        var imageForClip2 = new AiGeneratedImageInput
                            {
                                Prompt = "prompt 2",
                                Seed = 2099404,
                                AiMakeupId = makeup2.Id,
                                ShortPromptSummary = "summary for prompt 2",
                                Workflow = "image_wf2",
                                Files =
                                [
                                    JpegImage("main"),
                                    JpegImage("thumbnail128")
                                ],
                                Persons =
                                [
                                    new AiGeneratedImagePersonInput
                                    {
                                        ParticipantAiCharacterSelfieId = aiImage2.Id,
                                        Files =
                                        [
                                            JpegImage("thumbnail128"),
                                            JpegImage("cover"),
                                            JpegImage("main")
                                        ]
                                    }
                                ],
                                Sources =
                                [
                                    new AiGeneratedImageSourceInput
                                    {
                                        Type = AiGeneratedImageSourceType.Background,
                                        Files =
                                        [
                                            JpegImage("main"),
                                            JpegImage("thumbnail128")
                                        ]
                                    },
                                    new AiGeneratedImageSourceInput
                                    {
                                        Type = AiGeneratedImageSourceType.Outfit,
                                        Files =
                                        [
                                            JpegImage("main"),
                                            JpegImage("thumbnail128")
                                        ]
                                    }
                                ]
                            };
        var input = new AiGeneratedContentInput
                    {
                        Video = new AiGeneratedVideoInput
                                {
                                    Type = AiGeneratedVideoType.ImageToVideo,
                                    ExternalSongId = null,
                                    SongId = null,
                                    IsLipSync = false,
                                    Workflow = "video_wf",
                                    Files =
                                    [
                                        Mp4Video("main")
                                    ],
                                    Clips =
                                    [
                                        new AiGeneratedVideoClipInput
                                        {
                                            Type = AiGeneratedVideoType.Pan,
                                            Image = imageForClip1,
                                            Prompt = "Dancing in the dark",
                                            ShortPromptSummary = "smr",
                                            Seed = 750012,
                                            LengthSec = 10,
                                            Workflow = "clip_wf1",
                                            Files =
                                            [
                                                Mp4Video("main")
                                            ]
                                        },
                                        new AiGeneratedVideoClipInput
                                        {
                                            Type = AiGeneratedVideoType.Zoom,
                                            Image = imageForClip2,
                                            Prompt = "With you between my arms",
                                            ShortPromptSummary = "sskks",
                                            Seed = 475893,
                                            LengthSec = 5,
                                            Workflow = "clip_wf2",
                                            Files =
                                            [
                                                Mp4Video("main")
                                            ]
                                        }
                                    ]
                                }
                    };


        var saved = await service.SaveDraft(input);
        return saved;
    }
}