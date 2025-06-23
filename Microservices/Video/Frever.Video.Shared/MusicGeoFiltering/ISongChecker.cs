using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Videos.Shared.MusicGeoFiltering.AbstractApi;
using Microsoft.EntityFrameworkCore;

namespace Frever.Videos.Shared.MusicGeoFiltering;

public interface ISongChecker
{
    Task<SongLicenseStatus[]> CheckExternalSongLicense(string countryIso3Code, IEnumerable<long> externalSongIds);

    Task<SongByCountry[]> GetSongs(long[] ids);
}

public class FakeSongChecker : ISongChecker
{
    public Task<SongLicenseStatus[]> CheckExternalSongLicense(string countryIso3Code, IEnumerable<long> externalSongIds)
    {
        return Task.FromResult(externalSongIds.Select(id => SongLicenseStatus.Available).ToArray());
    }

    public Task<SongByCountry[]> GetSongs(long[] ids)
    {
        return Task.FromResult(Array.Empty<SongByCountry>());
    }
}

public class DbBasedSongChecker(CountryCodeLookup countryCodeLookup, IReadDb mainDb) : ISongChecker
{
    private readonly CountryCodeLookup _countryCodeLookup = countryCodeLookup ?? throw new ArgumentNullException(nameof(countryCodeLookup));
    private readonly IReadDb _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));

    public async Task<SongLicenseStatus[]> CheckExternalSongLicense(string countryIso3Code, IEnumerable<long> externalSongIds)
    {
        ArgumentNullException.ThrowIfNull(externalSongIds);

        if (string.IsNullOrWhiteSpace(countryIso3Code))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(countryIso3Code));

        if (!await _countryCodeLookup.IsMusicEnabled(countryIso3Code))
            return externalSongIds.Select(_ => SongLicenseStatus.Blocked).ToArray();

        var ids = externalSongIds.ToArray();

        var songs = await _mainDb.ExternalSongs.Where(s => ids.Contains(s.Id)).ToArrayAsync();

        return externalSongIds.Select(
                                   id =>
                                   {
                                       var song = songs.FirstOrDefault(s => s.Id == id);
                                       if (song == null)
                                           return SongLicenseStatus.Blocked;

                                       if (song.NotClearedSince != null || song.ExcludedCountries.Contains(countryIso3Code.ToLower()))
                                           return SongLicenseStatus.Blocked;

                                       return SongLicenseStatus.Available;
                                   }
                               )
                              .ToArray();
    }

    public Task<SongByCountry[]> GetSongs(long[] ids)
    {
        return _mainDb.Song.Where(e => ids.Contains(e.Id))
                      .Select(e => new SongByCountry {Id = e.Id, AvailableCountries = e.AvailableForCountries.ToHashSet()})
                      .ToArrayAsync();
    }
}

public class SongByCountry
{
    public long Id { get; init; }
    public HashSet<string> AvailableCountries { get; init; }
}