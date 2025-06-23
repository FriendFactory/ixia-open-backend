using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.CommercialMusic.LicenseChecking;

public interface IMusicLicenseCheckRepository
{
    IQueryable<ExternalSong> GetSongsCheckedBefore(DateTime checkTime);

    IQueryable<ExternalSong> GetById(long id);

    Task<(bool clearanceChanged, bool excludedCountriesChanged, bool isrcChanged)> UpdateExternalSong(ExternalSong song);
}

public class PersistentMusicLicenseCheckRepository(IWriteDb db) : IMusicLicenseCheckRepository
{
    public IQueryable<ExternalSong> GetSongsCheckedBefore(DateTime checkTime)
    {
        return db.ExternalSongs.Where(s => s.LastLicenseStatusCheckAt == null || s.LastLicenseStatusCheckAt < checkTime);
    }

    public IQueryable<ExternalSong> GetById(long id)
    {
        return db.ExternalSongs.Where(s => s.Id == id);
    }

    public async Task<(bool clearanceChanged, bool excludedCountriesChanged, bool isrcChanged)> UpdateExternalSong(ExternalSong song)
    {
        var original = await db.ExternalSongs.AsNoTracking().FirstOrDefaultAsync(s => s.Id == song.Id);

        var storedSong = await db.ExternalSongs.FindAsync(song.Id);

        var clearanceChanged = false;

        if ((original.NotClearedSince == null && song.NotClearedSince != null) ||
            (original.NotClearedSince != null && song.NotClearedSince == null))
        {
            clearanceChanged = true;
            storedSong.NotClearedSince = song.NotClearedSince != null ? DateTime.UtcNow : default(DateTime?);
        }

        var countriesBefore = NormalizeCountries(original.ExcludedCountries);
        var countriesAfter = NormalizeCountries(song.ExcludedCountries);


        var excludedCountriesChanged = false;
        if (countriesBefore.Length != countriesAfter.Length || countriesBefore.Zip(countriesAfter, (before, after) => (before, after))
                                                                              .Any(r => !StringComparer.Ordinal.Equals(r.before, r.after)))
        {
            excludedCountriesChanged = true;
            storedSong.ExcludedCountries = countriesAfter;
        }

        var isrcChanged = false;
        if (!StringComparer.OrdinalIgnoreCase.Equals(song.Isrc, original.Isrc))
        {
            isrcChanged = true;
            storedSong.Isrc = song.Isrc;
        }

        storedSong.LastLicenseStatusCheckAt = DateTime.UtcNow;
        storedSong.ArtistName = song.ArtistName;
        storedSong.SongName = song.SongName;
        storedSong.Isrc = song.Isrc;

        await db.SaveChangesAsync();

        return (clearanceChanged, excludedCountriesChanged, isrcChanged);
    }

    private static string[] NormalizeCountries(string[] countries)
    {
        return countries == null
                   ? Array.Empty<string>()
                   : countries.Select(c => c?.Trim().ToLowerInvariant())
                              .Where(c => !string.IsNullOrWhiteSpace(c))
                              .OrderBy(c => c)
                              .ToArray();
    }
}