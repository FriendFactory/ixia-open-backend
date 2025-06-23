using Common.Models.Files;
using FluentAssertions;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.Client.Core.IntegrationTest.Features.AI.Data;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.ClientService.Contract.Ai;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Client.Core.IntegrationTest.Features.AI;

public partial class AiContentTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task SaveContent_NewImage()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var makeup = await dataEnv.WithAiMakeUp();
        var (_, images) = await dataEnv.WithAiCharacters(user.MainGroupId);
        var aiImage = images[0];

        var service = provider.GetRequiredService<IAiGeneratedContentService>();

        var input = new AiGeneratedContentInput
                    {
                        Image = new AiGeneratedImageInput
                                {
                                    Prompt = "test prompt",
                                    Seed = 39394,
                                    AiMakeupId = makeup.Id,
                                    ShortPromptSummary = "summary",
                                    Workflow = "image_wf",
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

        dataEnv.Db.ChangeTracker.Clear();

        var read = await service.GetById(saved.Id);

        var dbContent = await dataEnv.Db.AiGeneratedContent.AsNoTracking().FirstOrDefaultAsync(i => i.Id == read.Id);
        var image = await dataEnv.Db.AiGeneratedImage.AsNoTracking().FirstOrDefaultAsync(i => i.Id == read.Image.Id);

        foreach (var dst in new[] {saved, read})
        {
            ShallowCompareAiContent(input, dst, user);
            DeepCompareAiImage(input.Image, dst.Image, user);
        }

        CompareFiles(read.Image, image);
    }

    [Fact]
    public async Task SaveContent_NewVideo()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var makeup1 = await dataEnv.WithAiMakeUp();
        var makeup2 = await dataEnv.WithAiMakeUp();
        var (_, images1) = await dataEnv.WithAiCharacters(user.MainGroupId);
        var (_, images2) = await dataEnv.WithAiCharacters(user.MainGroupId);
        var aiImage1 = images1[0];
        var aiImage2 = images2[0];

        var userSound = await dataEnv.WithUserSound(user.MainGroupId);

        var service = provider.GetRequiredService<IAiGeneratedContentService>();

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
                                            UserSoundId = userSound.Id,
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

        dataEnv.Db.ChangeTracker.Clear();

        var read = await service.GetById(saved.Id);

        foreach (var dst in new[] {saved, read})
        {
            ShallowCompareAiContent(input, dst, user);
            DeepCompareAiVideo(input.Video, dst.Video, user);
        }
    }

    [Fact]
    public async Task SaveContent_NewVideoUsingImageRefs()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var service = provider.GetRequiredService<IAiGeneratedContentService>();

        var image1ForClip1 = await CreateAiImage(dataEnv, user, service);
        var imageForClip2 = await CreateAiImage(dataEnv, user, service);

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
                                            RefAiImageId = image1ForClip1.Image.Id,
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
                                            RefAiImageId = imageForClip2.Image.Id,
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

        dataEnv.Db.ChangeTracker.Clear();

        var read = await service.GetById(saved.Id);

        foreach (var dst in new[] {saved, read})
        {
            ShallowCompareAiContent(input, dst, user);
            DeepCompareAiVideo(input.Video, dst.Video, user);
        }
    }

    [Fact]
    public async Task SaveContent_NewVideoWithoutImage()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var service = provider.GetRequiredService<IAiGeneratedContentService>();

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

        dataEnv.Db.ChangeTracker.Clear();

        var read = await service.GetById(saved.Id);

        foreach (var dst in new[] {saved, read})
        {
            ShallowCompareAiContent(input, dst, user);
            DeepCompareAiVideo(input.Video, dst.Video, user);
        }
    }


    private static void DeepCompareAiVideoClip(AiGeneratedVideoClipInput input, AiGeneratedVideoClipFullInfo saved, User currentUser)
    {
        input.Should().NotBeNull();
        saved.Should().NotBeNull();

        saved.Id.Should().NotBe(0);
        if (input.Id != 0)
            saved.Id.Should().Be(input.Id);

        saved.Prompt.Should().Be(input.Prompt);
        saved.ShortPromptSummary.Should().Be(input.ShortPromptSummary);
        saved.Seed.Should().Be(input.Seed);
        saved.LengthSec.Should().Be(input.LengthSec);
        saved.Workflow.Should().NotBeNull().And.Be(input.Workflow);
        saved.UserSoundId.Should().Be(input.UserSoundId);

        CompareFiles(input, saved);

        if (input.Image == null && input.RefAiImageId == null)
        {
            saved.Image.Should().BeNull();
        }
        else
        {
            if (input.Image != null)
                DeepCompareAiImage(input.Image, saved.Image, currentUser);
            else
                saved.Image.Id.Should().Be(input.RefAiImageId);
        }
    }

    private static FileMetadata JpegImage(string type)
    {
        return new FileMetadata {Type = type, Source = new FileSourceInfo {SourceBytes = "abt"u8.ToArray()}};
    }

    private static FileMetadata Mp4Video(string type)
    {
        return new FileMetadata
               {
                   Version = Guid.NewGuid().ToString(), Type = type, Source = new FileSourceInfo {SourceBytes = "abt"u8.ToArray()}
               };
    }

    private static void ShallowCompareAiContent(AiGeneratedContentInput input, AiGeneratedContentFullInfo saved, User currentUser)
    {
        input.Should().NotBeNull();
        saved.Should().NotBeNull();
        saved.Id.Should().NotBe(0);
        if (input.Id != 0)
            saved.Id.Should().Be(input.Id);

        (saved.Video == null).Should().Be(input.Video == null);
        (saved.Image == null).Should().Be(input.Image == null);
        (saved.Image != null || saved.Video != null).Should().BeTrue();

        saved.GroupId.Should().Be(currentUser.MainGroupId);
        saved.Type.Should().Be(input.Image == null ? AiGeneratedContentType.Video : AiGeneratedContentType.Image);
        saved.CreatedAt.Should().NotBe(default);
        saved.ExternalSongId.Should().Be(input.Video?.ExternalSongId);
        saved.IsLipSync.Should().Be(input.Video?.IsLipSync);
    }

    private static void DeepCompareAiImage(AiGeneratedImageInput input, AiGeneratedImageFullInfo saved, User currentUser)
    {
        saved.Should().NotBeNull();
        input.Should().NotBeNull();

        saved.Id.Should().NotBe(0);
        if (input.Id != 0)
            saved.Id.Should().Be(input.Id);

        saved.Prompt.Should().Be(input.Prompt);
        saved.Seed.Should().Be(input.Seed);
        saved.ShortPromptSummary.Should().Be(input.ShortPromptSummary);
        saved.GroupId.Should().Be(currentUser.MainGroupId);
        saved.AiMakeupId.Should().Be(input.AiMakeupId);
        saved.NumOfCharacters.Should().Be(input.Persons.Count);
        saved.Workflow.Should().NotBeNull().And.Be(input.Workflow);
        CompareFiles(input, saved);

        // Persons
        saved.Persons.Should().HaveSameCount(input.Persons);
        foreach (var src in input.Persons)
        {
            var matched = saved.Persons.SingleOrDefault(d => d.ParticipantAiCharacterSelfieId == src.ParticipantAiCharacterSelfieId);
            matched.Should().NotBeNull();
            matched.GenderId.Should().NotBe(0);
            matched.ParticipantGroupId.Should().NotBe(0);
            matched.ParticipantAiCharacterSelfieId.Should().Be(src.ParticipantAiCharacterSelfieId);
            matched.Id.Should().NotBe(0);
            if (src.Id != 0)
                matched.Id.Should().Be(src.Id);

            CompareFiles(src, matched);
        }

        // Sources
        input.Sources.Should().HaveSameCount(saved.Sources);
        foreach (var src in input.Sources)
        {
            var matched = saved.Sources.SingleOrDefault(s => s.Type == src.Type);
            matched.Should().NotBeNull();
            matched.Id.Should().NotBe(0);
            if (src.Id != 0)
                matched.Id.Should().Be(src.Id);

            matched.Type.Should().Be(src.Type);
            CompareFiles(src, matched);
        }
    }

    private static void DeepCompareAiVideo(AiGeneratedVideoInput input, AiGeneratedVideoFullInfo saved, User currentUser)
    {
        input.Should().NotBeNull();
        saved.Should().NotBeNull();

        saved.Id.Should().NotBe(0);
        if (input.Id != 0)
            saved.Id.Should().Be(input.Id);

        CompareFiles(input, saved);

        saved.GroupId.Should().Be(currentUser.MainGroupId);
        saved.ExternalSongId.Should().Be(input.ExternalSongId);
        saved.SongId.Should().Be(input.SongId);
        saved.IsLipSync.Should().Be(input.IsLipSync);
        saved.Type.Should().Be(input.Type);
        saved.Workflow.Should().NotBeNull().And.Be(input.Workflow);
        saved.LengthSec.Should().Be(input.Clips.Sum(c => c.LengthSec));

        var dstClips = saved.Clips.OrderBy(c => c.Ordinal).ToArray();
        for (var i = 0; i < dstClips.Length; i++)
        {
            var dstClip = dstClips[i];
            var srcClip = input.Clips[i];

            DeepCompareAiVideoClip(srcClip, dstClip, currentUser);
        }
    }

    private static void CompareFiles(IFileMetadataOwner input, IFileMetadataOwner saved)
    {
        input.Should().NotBeNull();
        saved.Should().NotBeNull();

        if (input.Id == 0)
        {
            saved.Files.Should().HaveCount(input.Files.Length);

            foreach (var src in input.Files)
            {
                var match = saved.Files.SingleOrDefault(dst => dst.Type == src.Type);
                match.Should().NotBeNull();

                match.Version.Should().NotBeNullOrWhiteSpace();

                (match.Path != null || match.Url != null).Should().BeTrue();

                if (match.Path != null)
                    match.Path.Should().NotBeNull().And.NotContain("/0/");
                else
                    match.Url.Should().NotBeNullOrWhiteSpace();

                match.Type.Should().NotBeNullOrWhiteSpace();
                match.Source.Should().BeNull();
            }
        }
        else
        {
            saved.Files.Should().HaveCountGreaterOrEqualTo(input.Files.Length);
            foreach (var src in input.Files)
            {
                var match = saved.Files.SingleOrDefault(dst => dst.Type == src.Type);
                match.Should().NotBeNull();

                match.Version.Should().NotBeNullOrWhiteSpace();

                (match.Path != null || match.Url != null).Should().BeTrue();

                if (match.Path != null)
                    match.Path.Should().NotBeNull().And.NotContain("/0/");
                else
                    match.Url.Should().NotBeNullOrWhiteSpace();

                match.Type.Should().NotBeNullOrWhiteSpace();
                match.Source.Should().BeNull();
            }
        }
    }
}