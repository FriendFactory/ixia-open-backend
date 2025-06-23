using Common.Models.Files;
using FluentAssertions;
using Frever.Client.Core.Features.Sounds.UserSounds;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.ClientService.Contract.Sounds;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Client.Core.IntegrationTest.Features.Assets.UserSounds;

public class UserSoundAssetServiceIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task UserSoundManaging_HappyPath()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var testService = provider.GetRequiredService<IUserSoundAssetService>();

        var noSounds = await testService.GetUserSoundListAsync(new UserSoundFilterModel {Take = 10});
        noSounds.Should().HaveCount(0);

        var addedSounds = new List<UserSoundFullInfo>();
        for (var i = 0; i < 20; i++)
            addedSounds.Add(
                await testService.SaveUserSound(
                    new UserSoundCreateModel
                    {
                        Duration = 10,
                        Name = $"test user sound {i}",
                        Size = 1002,
                        Files = [new FileMetadata {Type = "main", Source = new FileSourceInfo {SourceBytes = "abt"u8.ToArray()}}]
                    }
                )
            );

        var mySounds = await testService.GetUserSoundListAsync(new UserSoundFilterModel {Take = 10});
        mySounds.Should().HaveCount(10);
        mySounds.Should()
                .AllSatisfy(
                     s =>
                     {
                         var existing = addedSounds.Find(a => a.Id == s.Id);
                         existing.Should().NotBeNull("should return list of added user sounds");
                         existing.Should()
                                 .BeEquivalentTo(s, opts => opts.Excluding(a => a.Owner), "list item should have the same data as added");
                     }
                 );

        var page2 = await testService.GetUserSoundListAsync(new UserSoundFilterModel {Skip = 10, Take = 5});
        page2.Should().HaveCount(5);
        page2.Should()
             .AllSatisfy(
                  s =>
                  {
                      mySounds.Should().Match(_ => mySounds.All(i => i.Id != s.Id));

                      var existing = addedSounds.Find(a => a.Id == s.Id);
                      existing.Should().NotBeNull("should return list of added user sounds");
                      existing.Should()
                              .BeEquivalentTo(s, opts => opts.Excluding(a => a.Owner), "list item should have the same data as added");
                  }
              );
    }

    [Fact]
    public async Task UploadUserSound()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var testService = provider.GetRequiredService<IUserSoundAssetService>();
        var sound = await testService.SaveUserSound(
                        new UserSoundCreateModel
                        {
                            Duration = 10,
                            Name = "test user sound",
                            Size = 1002,
                            Files = [new FileMetadata {Type = "main", Source = new FileSourceInfo {SourceBytes = "abt"u8.ToArray()}}]
                        }
                    );

        var savedSound = await testService.GetUserSoundById(sound.Id);
        savedSound.Should().BeEquivalentTo(sound);

        var list = await testService.GetUserSoundListAsync(new UserSoundFilterModel {Skip = 0, Take = 10});
        list.Should().HaveCount(1);
        list[0]
           .Should()
           .BeEquivalentTo(
                savedSound,
                opts =>
                {
                    opts.Excluding(e => e.Owner);
                    return opts;
                }
            );
    }

    [Fact]
    public async Task UserSound_Rename()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var testService = provider.GetRequiredService<IUserSoundAssetService>();
        var sound = await testService.SaveUserSound(
                        new UserSoundCreateModel
                        {
                            Duration = 10,
                            Name = "test user sound",
                            Size = 1002,
                            Files = [new FileMetadata {Type = "main", Source = new FileSourceInfo {SourceBytes = "abt"u8.ToArray()}}]
                        }
                    );

        await testService.RenameUserSound(sound.Id, "new name");

        var savedSound = await testService.GetUserSoundById(sound.Id);
        savedSound.Should().BeEquivalentTo(sound, opt => opt.Excluding(s => s.Name));
        savedSound.Name.Should().Be("new name");
    }
}