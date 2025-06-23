using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frever.Videos.Shared.MusicGeoFiltering;

public interface IMusicGeoFilter
{
    /// <summary>
    ///     From the given list of external songs finds those are not available in the given country.
    /// </summary>
    Task<ISet<long>> FindUnavailableExternalSongs(string countryIso3Code, ISet<long> ids);

    Task<ISet<long>> FindUnavailableSongs(string countryIso3Code, ISet<long> ids);

    Task ResetSongInfo(long externalSongId);
}

public static class MusicLicenseGeoFilterExtensions
{
    public static async Task<T[]> FilterOutUnavailableSongs<T>(
        this IMusicGeoFilter musicGeoFilter,
        string currentUserLocation,
        IEnumerable<T> source,
        Func<T, long[]> getExternalSongs,
        Func<T, long[]> getSongs
    )
    {
        ArgumentNullException.ThrowIfNull(musicGeoFilter);
        ArgumentNullException.ThrowIfNull(source);

        getExternalSongs ??= _ => [];
        getSongs ??= _ => [];

        var all = source.Select(s => new {Item = s, ExternalSongs = getExternalSongs(s), Songs = getSongs(s)}).ToArray();

        var allExternalSongs = all.Where(s => s.ExternalSongs is {Length: > 0}).SelectMany(s => s.ExternalSongs).Distinct().ToHashSet();

        var blocked = await musicGeoFilter.FindUnavailableExternalSongs(currentUserLocation, allExternalSongs);
        if (blocked.Count > 0)
            all = all.Where(
                          t => t.ExternalSongs == null || t.ExternalSongs.Length == 0 || !t.ExternalSongs.Any(sid => blocked.Contains(sid))
                      )
                     .ToArray();

        var allSongs = all.Where(s => s.Songs is {Length: > 0}).SelectMany(s => s.Songs).Distinct().ToHashSet();

        var blockedSongs = await musicGeoFilter.FindUnavailableSongs(currentUserLocation, allSongs);
        if (blockedSongs.Count > 0)
            all = all.Where(t => t.Songs == null || t.Songs.Length == 0 || !t.Songs.Any(sid => blockedSongs.Contains(sid))).ToArray();

        return all.Select(a => a.Item).ToArray();
    }

    public static async Task<bool> AreAnySongUnavailable(
        this IMusicGeoFilter musicGeoFilter,
        string currentUserLocation,
        IEnumerable<long> externalSongIds,
        IEnumerable<long> songIds
    )
    {
        ArgumentNullException.ThrowIfNull(musicGeoFilter);

        var blocked = await musicGeoFilter.FindUnavailableExternalSongs(currentUserLocation, externalSongIds?.ToHashSet());
        if (blocked is {Count: > 0})
            return true;

        var blockedSongs = await musicGeoFilter.FindUnavailableSongs(currentUserLocation, songIds?.ToHashSet());
        if (blockedSongs is {Count: > 0})
            return true;

        return false;
    }
}