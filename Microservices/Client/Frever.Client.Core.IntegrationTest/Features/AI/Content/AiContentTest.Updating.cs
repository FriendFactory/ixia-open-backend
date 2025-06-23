using FluentAssertions;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.Client.Core.IntegrationTest.Features.AI.Data;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Frever.Client.Core.IntegrationTest.Features.AI;

public partial class AiContentTest
{
    [Fact]
    public async Task SaveContent_UpdateImage()
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

        var existing = await CreateAiImage(dataEnv, user, service);

        var input = new AiGeneratedContentInput
                    {
                        Id = existing.Id,
                        Image = new AiGeneratedImageInput
                                {
                                    Id = existing.Image.Id,
                                    Files = [],
                                    Persons = existing.Image.Persons.Select(
                                                           p => new AiGeneratedImagePersonInput
                                                                {
                                                                    Files = p.Files,
                                                                    Id = p.Id,
                                                                    ParticipantAiCharacterSelfieId = p.ParticipantAiCharacterSelfieId
                                                                }
                                                       )
                                                      .ToList(),
                                    Prompt = existing.Image.Prompt + " changed",
                                    Seed = existing.Image.Seed * 10,
                                    Sources =
                                        existing.Image.Sources.Select(
                                                     s => new AiGeneratedImageSourceInput {Files = [], Id = s.Id, Type = s.Type}
                                                 )
                                                .ToList(),
                                    Workflow = existing.Image.Workflow + " changed wf",
                                    AiMakeupId = existing.Image.AiMakeupId,
                                    ShortPromptSummary = existing.Image.ShortPromptSummary + " changed summary"
                                }
                    };

        var saved = await service.SaveDraft(input);

        saved.Image.Files.Should().HaveCount(existing.Image.Files.Length);

        dataEnv.Db.ChangeTracker.Clear();

        var read = await service.GetById(existing.Id);
        ShallowCompareAiContent(input, read, user);
        DeepCompareAiImage(input.Image, read.Image, user);
    }
}