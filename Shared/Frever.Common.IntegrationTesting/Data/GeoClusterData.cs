using Common.Infrastructure.Utils;
using FluentAssertions;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Common.IntegrationTesting.Data;

public static class GeoClusterData
{
    public static async Task ClearGeoClusters(this DataEnvironment dataEnvironment)
    {
        ArgumentNullException.ThrowIfNull(dataEnvironment);
        await dataEnvironment.Db.Database.ExecuteSqlRawAsync("delete from \"GeoCluster\"");
    }

    public static async Task<GeoCluster[]> WithGeoClusters(this DataEnvironment dataEnvironment, params GeoClusterInput[] geoClusters)
    {
        ArgumentNullException.ThrowIfNull(dataEnvironment);
        ArgumentNullException.ThrowIfNull(geoClusters);

        if (geoClusters.All(e => e.Priority == 0))
            geoClusters.ForEach((e, index) => e.Priority = geoClusters.Length - index + 1);

        return await dataEnvironment.WithEntityCollection<GeoCluster>("create-geo-clusters", geoClusters);
    }

    public static async Task<(GeoCluster swe, GeoCluster usa)> WithTwoGeoClusters(this DataEnvironment dataEnvironment)
    {
        ArgumentNullException.ThrowIfNull(dataEnvironment);

        await dataEnvironment.ClearGeoClusters();

        var clusters = await dataEnvironment.WithGeoClusters(
                           new GeoClusterInput
                           {
                               Title = "Swedish",
                               IncludeVideosFromCountry = ["swe"],
                               IncludeVideosWithLanguage = ["swe"],
                               ShowToUserFromCountry = ["swe"],
                               ShowToUserWithLanguage = ["swe"]
                           },
                           new GeoClusterInput
                           {
                               Title = "Americal",
                               IncludeVideosFromCountry = ["usa"],
                               IncludeVideosWithLanguage = ["eng"],
                               ShowToUserFromCountry = ["usa"],
                               ShowToUserWithLanguage = ["eng"]
                           }
                       );

        return (clusters[0], clusters[1]);
    }

    public static void AssertGeoCluster<TEntity>(
        this DataEnvironment dataEnvironment,
        IEnumerable<TEntity> input,
        GeoCluster geoCluster,
        Func<TEntity, (string country, string language)> getGeoInfo
    )
    {
        ArgumentNullException.ThrowIfNull(dataEnvironment);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(geoCluster);
        ArgumentNullException.ThrowIfNull(getGeoInfo);

        foreach (var element in input)
        {
            var (country, language) = getGeoInfo(element);

            geoCluster.IncludeVideoFromCountry.Should()
                      .Match(c => c.Contains(country, StringComparer.OrdinalIgnoreCase) || c.Contains("*"));
            geoCluster.ExcludeVideoFromCountry.Should()
                      .Match(c => !c.Contains(country, StringComparer.OrdinalIgnoreCase) && !c.Contains("*"));

            geoCluster.IncludeVideoWithLanguage.Should()
                      .Match(l => l.Contains(language, StringComparer.OrdinalIgnoreCase) || l.Contains("*"));
            geoCluster.ExcludeVideoWithLanguage.Should()
                      .Match(l => !l.Contains(language, StringComparer.OrdinalIgnoreCase) && !l.Contains("*"));
        }
    }
}

public class GeoClusterInput
{
    public int Priority { get; set; }
    public string Title { get; set; }
    public string[] IncludeVideosFromCountry { get; set; } = [];
    public string[] IncludeVideosWithLanguage { get; set; } = [];
    public string[] ShowToUserFromCountry { get; set; } = [];
    public string[] ShowToUserWithLanguage { get; set; } = [];
}