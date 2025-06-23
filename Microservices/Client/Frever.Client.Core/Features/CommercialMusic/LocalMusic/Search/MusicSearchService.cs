using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Videos.Shared.MusicGeoFiltering;
using Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Core.Features.CommercialMusic;

public class MusicSearchService(CountryCodeLookup country, IReadDb mainDb, ICurrentLocationProvider location, ILoggerFactory loggerFactory)
    : IMusicSearchService
{
    private const int MinimalSearchLength = 3;

    private readonly CountryCodeLookup _country = country ?? throw new ArgumentNullException(nameof(country));
    private readonly ICurrentLocationProvider _location = location ?? throw new ArgumentNullException(nameof(location));
    private readonly IReadDb _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));

    private readonly ILogger _log = loggerFactory.CreateLogger("Frever.LocalMusicSearch");

    public async Task<TrackInfo[]> Search(string q, int skip, int take)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < MinimalSearchLength)
            return [];

        take = Math.Clamp(take, 1, 50);

        var country = (await _location.Get()).CountryIso3Code;
        if (!await _country.IsMusicEnabled(country))
        {
            _log.LogWarning("Music is not enabled in the country {c}", country);
            return [];
        }

        var sw = Stopwatch.StartNew();

        var equalCount = await SearchDtoEqualCount(q, country);
        var equalResults = Array.Empty<ExternalSongSearchDto>();
        if (equalCount > skip)
        {
            equalResults = await LoadSearchDtoEqual(q, skip, take, country);
        }

        if (equalResults.Length >= take)
        {
            sw.Stop();
            _log.LogInformation("Searching for ({q}) took {et}", q, sw.Elapsed);
            return equalResults.Select(
                                    a => new TrackInfo
                                         {
                                             Artist = a.ArtistName,
                                             Isrc = a.Isrc,
                                             Key = a.ExternalTrackId.ToString(),
                                             Title = a.SongName,
                                             ExternalTrackId = a.ExternalTrackId
                                         }
                                )
                               .ToArray();
        }

        var fuzzyCount = await SearchDtoFuzzyCount(q, country);

        var fuzzySkip = Math.Max(0, skip - equalCount);
        var fuzzyTake = Math.Max(0, take - equalResults.Length);
        var fuzzyResults = Array.Empty<ExternalSongSearchDto>();
        if (fuzzyTake > 0)
        {
            fuzzyResults = await LoadSearchDtoFuzzy(q, fuzzySkip, fuzzyTake, country);
        }

        var allResults = equalResults.Concat(fuzzyResults).ToArray();
        if (allResults.Length >= take)
        {
            sw.Stop();
            _log.LogInformation("Searching for ({q}) took {et}", q, sw.Elapsed);
            return allResults.Select(
                                  a => new TrackInfo
                                       {
                                           Artist = a.ArtistName,
                                           Isrc = a.Isrc,
                                           Key = a.ExternalTrackId.ToString(),
                                           Title = a.SongName,
                                           ExternalTrackId = a.ExternalTrackId
                                       }
                              )
                             .ToArray();
        }

        var popularitySkip = Math.Max(0, skip - equalCount - fuzzyCount);
        var popularityTake = Math.Max(0, take - allResults.Length);
        var popularityResults = await SearchDtoOrderBySpotifyPopularity(popularitySkip, popularityTake, country);

        allResults = allResults.Concat(popularityResults).ToArray();
        sw.Stop();
        _log.LogInformation("Searching for ({q}) took {et}", q, sw.Elapsed);

        return allResults.Select(
                              a => new TrackInfo
                                   {
                                       Artist = a.ArtistName,
                                       Isrc = a.Isrc,
                                       Key = a.ExternalTrackId.ToString(),
                                       Title = a.SongName,
                                       ExternalTrackId = a.ExternalTrackId
                                   }
                          )
                         .ToArray();
    }

    private Task<ExternalSongSearchDto[]> LoadSearchDtoFuzzy(string q, int skip, int take, string country)
    {
        FormattableString sql = $"""
                                     select * from
                                       (
                                       select distinct on (2) "ExternalTrackId", "Isrc", "SongName", "ArtistName", "SpotifyPopularity"
                                         from "ExternalSong"
                                         where (f_unaccent("SongName") ilike {'%' + q + '%'} or f_unaccent("ArtistName") ilike {'%' + q + '%'})
                                             and not ("SongName" = {'%' + q + '%'} or "ArtistName" = {'%' + q + '%'})
                                             and not "IsDeleted" and not "IsManuallyDeleted" and "NotClearedSince" is null
                                             and not lower({country}) = any(lower("ExcludedCountries"::text)::text[])
                                         order by 2, 1
                                       ) songs order by "SpotifyPopularity" desc nulls last, "SongName" nulls last, "ArtistName" nulls last
                                         limit {take} offset {skip}
                                 """;
        return _mainDb.SqlQuery<ExternalSongSearchDto>(sql).ToArrayAsync();
    }

    private Task<int> SearchDtoFuzzyCount(string q, string country)
    {
        FormattableString sql = $"""
                                       select distinct on (2) "ExternalTrackId", "Isrc"
                                         from "ExternalSong"
                                         where (f_unaccent("SongName") ilike {'%' + q + '%'} or f_unaccent("ArtistName") ilike {'%' + q + '%'})
                                             and not ("SongName" = {'%' + q + '%'} or "ArtistName" = {'%' + q + '%'})
                                             and not "IsDeleted" and not "IsManuallyDeleted" and "NotClearedSince" is null
                                             and not lower({country}) = any(lower("ExcludedCountries"::text)::text[])
                                         order by 2, 1
                                 """;
        return _mainDb.SqlQuery<int>(sql).CountAsync();
    }

    private Task<ExternalSongSearchDto[]> LoadSearchDtoEqual(string q, int skip, int take, string country)
    {
        FormattableString sql = $"""
                                     select * from
                                       (
                                       select distinct on (2) "ExternalTrackId", "Isrc", "SongName", "ArtistName", "SpotifyPopularity"
                                         from "ExternalSong"
                                         where ("SongName" = {q} or "ArtistName" = {q})
                                             and not "IsDeleted" and not "IsManuallyDeleted" and "NotClearedSince" is null
                                             and not lower({country}) = any(lower("ExcludedCountries"::text)::text[])
                                             order by 2, 1
                                       ) songs order by "SpotifyPopularity" desc nulls last, "SongName" nulls last, "ArtistName" nulls last
                                         limit {take} offset {skip}
                                 """;
        return _mainDb.SqlQuery<ExternalSongSearchDto>(sql).ToArrayAsync();
    }

    private Task<int> SearchDtoEqualCount(string q, string country)
    {
        FormattableString sql = $"""
                                       select distinct on (2) "ExternalTrackId", "Isrc"
                                         from "ExternalSong"
                                         where ("SongName" = {q} or "ArtistName" = {q})
                                             and not "IsDeleted" and not "IsManuallyDeleted" and "NotClearedSince" is null
                                             and not lower({country}) = any(lower("ExcludedCountries"::text)::text[])
                                             order by 2, 1
                                 """;
        return _mainDb.SqlQuery<int>(sql).CountAsync();
    }

    private Task<ExternalSongSearchDto[]> SearchDtoOrderBySpotifyPopularity(int skip, int take, string country)
    {
        FormattableString sql = $"""
                                     select * from (
                                         select distinct on (2) es."ExternalTrackId", es."Isrc", es."SongName", es."ArtistName", es."SpotifyPopularity"
                                         from (
                                             select distinct("Isrc"), "SpotifyPopularity", "SongName", "ArtistName" from "ExternalSong"
                                             where not "IsDeleted" and not "IsManuallyDeleted" and "NotClearedSince" is null and "SpotifyPopularity" is not null
                                             and not lower({country}) = any(lower("ExcludedCountries"::text)::text[])
                                             order by "SpotifyPopularity" desc, "SongName" nulls last, "ArtistName" nulls last
                                             limit {take} offset {skip}
                                         ) isrcs inner join "ExternalSong" es on isrcs."Isrc" = es."Isrc"
                                             where not es."IsDeleted" and not es."IsManuallyDeleted" and es."NotClearedSince" is null
                                             order by 2, 1
                                     ) songs order by "SpotifyPopularity" desc, "SongName" nulls last, "ArtistName" nulls last;
                                 """;
        return _mainDb.SqlQuery<ExternalSongSearchDto>(sql).ToArrayAsync();
    }
}