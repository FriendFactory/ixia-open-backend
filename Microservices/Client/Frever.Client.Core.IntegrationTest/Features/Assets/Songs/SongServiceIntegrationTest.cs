using FluentAssertions;
using Frever.Client.Core.Features.Sounds.Song;
using Frever.Client.Core.IntegrationTest.Utils;
using Frever.ClientService.Contract.Sounds;
using Frever.Common.IntegrationTesting;
using Frever.Common.IntegrationTesting.Data;
using Frever.Common.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Frever.Client.Core.IntegrationTest.Features.Assets.Songs;

public class SongServiceIntegrationTest(ITestOutputHelper testOut) : IntegrationTestBase
{
    [Fact]
    public async Task Song_Reading()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var testService = provider.GetRequiredService<ISongAssetService>();

        var page = await testService.GetSongListAsync(new SongFilterModel {Take = 10});
        page.Should().NotBeNull();
        page.Should().HaveCount(10);

        page.Should()
            .AllSatisfy(
                 b =>
                 {
                     b.Id.Should().NotBe(0);
                     b.Name.Should().NotBeNull().And.NotBeEmpty();
                     b.Files.Should().NotBeNull();
                     b.Files.Should().HaveCount(4);
                     b.Files.Should()
                      .AllSatisfy(
                           f =>
                           {
                               f.Path.Should().NotBeNull().And.NotBeEmpty();
                               f.Type.Should().NotBeNull();
                               f.Url.Should().NotBeNull().And.NotBeEmpty();
                               f.Version.Should().NotBeNull().And.NotBeEmpty();
                           }
                       );
                 }
             );

        var page2 = await testService.GetSongListAsync(new SongFilterModel {Skip = 10, Take = 10});
        page2.Should().NotBeNull();
        page2.Should().HaveCount(10);

        page2.Should()
             .AllSatisfy(
                  b =>
                  {
                      b.Id.Should().NotBe(0);
                      b.Name.Should().NotBeNull().And.NotBeEmpty();
                      b.Id.Should().Match(id => page.All(i => i.Id != id));
                  }
              );
    }

    [Fact]
    public async Task Song_Filtering()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        var testService = provider.GetRequiredService<ISongAssetService>();

        var byName = await testService.GetSongListAsync(new SongFilterModel {Name = "breath", Take = 10});
        byName.Should().NotBeNull();
        byName.Should().HaveCountGreaterThanOrEqualTo(1, "filtering by name should work");
        byName.Should()
              .AllSatisfy(cf => { cf.Name.Should().Match(n => n.Contains("breath", StringComparison.InvariantCultureIgnoreCase)); });

        var byGenre = await testService.GetSongListAsync(new SongFilterModel {GenreId = 30, Take = 10});
        byGenre.Should().NotBeNull();
        byGenre.Should().HaveCount(10, "filtering by genre should work");
        byGenre.Should().AllSatisfy(s => { s.GenreId.Should().Be(30, "filtered by genre=10"); });

        var commercialOnly = await testService.GetSongListAsync(new SongFilterModel {CommercialOnly = true, Take = 3});
        commercialOnly.Should().NotBeNull();
        commercialOnly.Should().HaveCount(3, "filtering by commercial only should work");

        long[] ids = [1442, 1352, 1219, 1158];
        var byIds = await testService.GetSongListAsync(new SongFilterModel {Ids = ids, Take = 20});
        byIds.Should().NotBeNull();
        byIds.Should().HaveCount(ids.Length, "filtering by ids should work");
        byIds.Should().AllSatisfy(s => { s.Id.Should().Match(id => ids.Contains(id), "filtered by set of ID"); });

        var byIds2 = await testService.GetSongs(ids);
        byIds2.Should().BeEquivalentTo(byIds, opts => opts.ExcludingMissingMembers());
    }

    [Fact]
    public async Task Song_PromotedSong()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddClientIntegrationTests(testOut);

        await using var provider = services.BuildServiceProvider();

        var dataEnv = provider.GetRequiredService<DataEnvironment>();

        var user = await dataEnv.WithUserAndGroup(new UserAndGroupCreateParams {CountryIso3 = "swe", LanguageIso3 = "swe"});
        provider.SetCurrentUser(user);

        await dataEnv.WithRandomPromotedSong(10);

        var testService = provider.GetRequiredService<ISongAssetService>();

        var promoted = await testService.GetPromotedSongs(0, 10);
        promoted.Should().NotBeNull();
        promoted.Should().HaveCount(10);
    }
}