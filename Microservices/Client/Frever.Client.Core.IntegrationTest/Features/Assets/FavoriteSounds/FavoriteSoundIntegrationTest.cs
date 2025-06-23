using Common.Infrastructure.Sounds;
using Common.Models.Files;
using FluentAssertions;
using Frever.Client.Core.Features.Sounds.FavoriteSounds;
using Frever.Client.Core.Features.Sounds.Song;
using Frever.Client.Core.Features.Sounds.UserSounds;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.ClientService.Contract.Sounds;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Client.Core.IntegrationTest.Features.Assets.FavoriteSounds;

public class FavoriteSoundIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task FavoriteSoundManaging_HappyPath()
    {
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var testService = provider.GetRequiredService<IFavoriteSoundService>();

        var noFavorite = await testService.GetMyFavoriteSounds(false, 0, 10);
        noFavorite.Should().HaveCount(0);

        var songService = provider.GetRequiredService<ISongAssetService>();
        var songs = await songService.GetSongListAsync(new SongFilterModel {Take = 30});
        songs.Should().HaveCountGreaterThan(0, "can't test adding favorite song otherwise");

        var userSoundService = provider.GetRequiredService<IUserSoundAssetService>();
        await userSoundService.SaveUserSound(
            new UserSoundCreateModel
            {
                Duration = 10,
                Name = "test user sound",
                Size = 1002,
                Files = [new FileMetadata {Type = "main", Source = new FileSourceInfo {SourceBytes = "abt"u8.ToArray()}}]
            }
        );
        var userSounds = await userSoundService.GetUserSoundListAsync(new UserSoundFilterModel {Take = 10});
        userSounds.Should().HaveCountGreaterThan(0, "can't test adding favorite user sound otherwise");

        await testService.AddFavoriteSound(songs[0].Id, FavoriteSoundType.Song);
        await testService.AddFavoriteSound(userSounds[0].Id, FavoriteSoundType.UserSound);

        var myFavorites = await testService.GetMyFavoriteSounds(false, 0, 10);
        myFavorites.Should().HaveCount(2);
        myFavorites.Should().Match(coll => coll.Any(e => e.Id == songs[0].Id && e.Type == FavoriteSoundType.Song));
        myFavorites.Should().Match(coll => coll.Any(e => e.Id == userSounds[0].Id && e.Type == FavoriteSoundType.UserSound));

        await testService.RemoveFavoriteSound(userSounds[0].Id, FavoriteSoundType.UserSound);

        var myNewFavorites = await testService.GetMyFavoriteSounds(false, 0, 10);
        myNewFavorites.Should().HaveCount(1);
        myNewFavorites.Should().Match(coll => coll.Any(e => e.Id == songs[0].Id && e.Type == FavoriteSoundType.Song));

        // Test pagination
        foreach (var s in songs.Skip(1))
            await testService.AddFavoriteSound(s.Id, FavoriteSoundType.Song);


        var firstPage = await testService.GetMyFavoriteSounds(false, 0, 5);
        firstPage.Should().HaveCount(5);
        var nextPage = await testService.GetMyFavoriteSounds(false, firstPage.Length, 5);
        nextPage.Should().HaveCount(5);
        nextPage.Should()
                .AllSatisfy(
                     fs => { firstPage.Should().Match(c => c.All(item => item.Key != fs.Key)); },
                     "elements in next page should be different from elements in first page"
                 );
    }
}