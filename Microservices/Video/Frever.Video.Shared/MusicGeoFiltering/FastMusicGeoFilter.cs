using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Frever.Videos.Shared.MusicGeoFiltering;

public class FastMusicGeoFilter : IMusicGeoFilter
{
    public static readonly string SongKeyPrefix = "song-availability::by-country".FreverVersionedCache();
    private static readonly string KeyPrefix = "music-availability::by-country".FreverVersionedCache();

    private readonly ICache _cache;
    private readonly ILogger _logger;
    private readonly IDatabase _redis;
    private readonly ISongChecker _songChecker;

    public FastMusicGeoFilter(IConnectionMultiplexer redisConnection, ISongChecker songChecker, ICache cache, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(redisConnection);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _songChecker = songChecker ?? throw new ArgumentNullException(nameof(songChecker));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _redis = redisConnection.GetDatabase();
        _logger = loggerFactory.CreateLogger("Frever.MusicLicenseCheck");
    }

    public async Task<ISet<long>> FindUnavailableSongs(string countryIso3Code, ISet<long> ids)
    {
        if (ids == null || ids.Count == 0)
            return ids;

        using var scope = _logger.BeginScope("[{Rid}]: ", Guid.NewGuid().ToString("N"));

        _logger.LogInformation("Check song unavailability: iso={Iso} songs={Ids}", countryIso3Code, string.Join(",", ids));

        if (string.IsNullOrWhiteSpace(countryIso3Code) || StringComparer.Ordinal.Equals(
                ICurrentLocationProvider.UnknownLocationFakeIso3Code,
                countryIso3Code
            ))
        {
            _logger.LogInformation("Country code is empty or fake, allow all songs");
            return ids;
        }

        countryIso3Code = countryIso3Code.ToLowerInvariant();

        var songs = await _cache.GetOrCacheFromHash(SongKeyPrefix, songIds => _songChecker.GetSongs(songIds), e => e.Id, ids.ToArray());

        return songs.Values.Where(e => e.AvailableCountries is {Count: > 0} && !e.AvailableCountries.Contains(countryIso3Code))
                    .Select(e => e.Id)
                    .Distinct()
                    .ToHashSet();
    }

    public async Task<ISet<long>> FindUnavailableExternalSongs(string countryIso3Code, ISet<long> ids)
    {
        if (ids == null || ids.Count == 0)
            return ids;

        using var scope = _logger.BeginScope("[{Rid}]: ", Guid.NewGuid().ToString("N"));

        _logger.LogInformation("Check external song unavailability: iso={Iso} songs={Ids}", countryIso3Code, string.Join(",", ids));

        if (string.IsNullOrWhiteSpace(countryIso3Code) || StringComparer.Ordinal.Equals(
                ICurrentLocationProvider.UnknownLocationFakeIso3Code,
                countryIso3Code
            ))
        {
            _logger.LogInformation("Country code is empty or fake, allow all songs");
            return ids;
        }

        countryIso3Code = countryIso3Code.ToLowerInvariant();

        var result = new HashSet<long>(ids.Count);

        var songBlockInfo = _redis.HashGet(MusicCountryAvailabilityKey(countryIso3Code), ids.Select(i => (RedisValue) i).ToArray());

        var nonCheckedSongs = new List<long>(ids.Count);

        {
            var i = 0;
            foreach (var id in ids)
            {
                var songStatus = songBlockInfo[i];

                if (songStatus == RedisValue.Null) // Not yet checked
                {
                    nonCheckedSongs.Add(id);
                }
                else
                {
                    var status = (byte[]) songStatus;
                    if (status[0] == (byte) SongLicenseStatus.Blocked)
                        result.Add(id);
                }

                i++;
            }
        }

        _logger.LogInformation("Cached unavailable songs: {Sid}", string.Join(",", result));

        if (nonCheckedSongs.Count != 0)
            await CheckSongsAndCacheResult(countryIso3Code, nonCheckedSongs, result);

        _logger.LogInformation("Unavailable songs: {Sid}", string.Join(",", result));

        return result;
    }

    public async Task ResetSongInfo(long externalSongId)
    {
        var keys = await _cache.GetKeysByPrefix(KeyPrefix);
        foreach (var key in keys)
            _redis.HashDelete(key, externalSongId);
    }

    private static string MusicCountryAvailabilityKey(string countryIso3Code)
    {
        if (string.IsNullOrWhiteSpace(countryIso3Code))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(countryIso3Code));

        return $"{KeyPrefix}::{countryIso3Code}::{DateTime.UtcNow.ToShortDateString()}";
    }

    private async Task CheckSongsAndCacheResult(
        string countryIso3Code,
        List<long> nonCheckedSongs,
        HashSet<long> unavailableSongsAccumulator
    )
    {
        var nonCheckedSongResults = await _songChecker.CheckExternalSongLicense(countryIso3Code, nonCheckedSongs);

        var wereKeyPresented = _redis.KeyExists(MusicCountryAvailabilityKey(countryIso3Code));

        _redis.HashSet(
            MusicCountryAvailabilityKey(countryIso3Code),
            nonCheckedSongs.Select((id, j) => new HashEntry(id, new[] {(byte) nonCheckedSongResults[j]})).ToArray()
        );

        if (!wereKeyPresented)
        {
            var expireNextDay = DateTime.UtcNow.AddDays(1).Date.AddMinutes(10);
            _redis.KeyExpire(MusicCountryAvailabilityKey(countryIso3Code), expireNextDay);
        }

        var j = 0;

        foreach (var id in nonCheckedSongs)
        {
            var songStatus = nonCheckedSongResults[j];

            if (songStatus == SongLicenseStatus.Blocked)
                unavailableSongsAccumulator.Add(id);

            j++;
        }
    }
}