using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using CsvHelper;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Core.Features.CommercialMusic;

public interface ISpotifyPopularityService
{
    Task RefreshSpotifyPopularity();
}

public class SpotifyPopularityService : ISpotifyPopularityService
{
    private const int UpdateSpotifyPopularityInDbBatchSize = 100;

    private readonly ILogger _log;
    private readonly IWriteDb _mainDb;
    private readonly IAmazonS3 _s3;
    private readonly SpotifyPopularitySettings _settings;

    public SpotifyPopularityService(SpotifyPopularitySettings settings, ILoggerFactory loggerFactory, IAmazonS3 s3, IWriteDb mainDb)
    {
        if (loggerFactory == null)
            throw new ArgumentNullException(nameof(loggerFactory));
        _settings = settings;
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));
        _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));
        _log = loggerFactory.CreateLogger("Frever.Spotify");
    }

    /// Bucket structure is next:
    /// -- a set of daily files with some parts of index. Those files has name `spotify_popularity_
    /// <yyyy_MM_dd_HH_mm_ss>
    ///     .csv`
    ///     -- optional full index at some date. Named spotify_popularity_full.csv but that name should be get from settings
    ///     To load popularity, we do next step
    ///     -- determine if we ever load full index for environment (check if there are non-null popularity values in db)
    ///     -- if not -- load full index
    ///     -- load list of files for last month
    public async Task RefreshSpotifyPopularity()
    {
        _log.LogInformation("Begin updating Spotify popularity");
        var (incremental, full) = await LoadPopularityIndexPaths();

        _log.LogInformation("Full index key: {}", full ?? "<missing>");
        _log.LogInformation("Incremental index keys: {}", string.Join(Environment.NewLine, incremental));

        var needFull = await NeedToLoadFullIndex();
        if (needFull)
        {
            if (string.IsNullOrWhiteSpace(full))
            {
                _log.LogWarning("Environment doesn't yet have spotify popularity info but full index is missing");
            }
            else
            {
                var fullIndex = await LoadIndex(full);
                _log.LogInformation("Full index loaded, {Count} records", fullIndex.Count);

                await UpdatePopularity(fullIndex);
            }
        }

        // Sergii: assume index keys contain dates and therefore ordered correctly
        // from older to newer
        foreach (var index in incremental)
        {
            _log.LogInformation("Start loading incremental index {Index}", index);
            var data = await LoadIndex(index);
            _log.LogInformation("{Index} loaded, contains {Count} records", index, data.Count);

            await UpdatePopularity(data);
            _log.LogInformation("Index {Index} loaded successfully", index);
        }
    }

    private async Task<(List<string> filesToLoad, string fullFile)> LoadPopularityIndexPaths()
    {
        _log.LogInformation("Load list of files from S3 to parse, bucket={Bucket} prefix={Prefix}", _settings.Bucket, _settings.Prefix);

        var monthAgoPrefix = JoinPath(_settings.Prefix, $"spotify_popularity_{DateTime.Today.AddDays(-10).ToString("yyyy-MM-dd")}");

        var recentIndexes = await GetS3KeysAfter(_settings.Prefix, monthAgoPrefix);
        var fullIndex = (await GetS3Keys(JoinPath(_settings.Prefix, _settings.FullDataCsvFileName))).FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(fullIndex))
            recentIndexes = recentIndexes.Where(a => !StringComparer.Ordinal.Equals(a, fullIndex)).ToList();

        return (recentIndexes, fullIndex);
    }

    private async Task<List<string>> GetS3Keys(string prefix)
    {
        var response = await _s3.ListObjectsV2Async(new ListObjectsV2Request {BucketName = _settings.Bucket, Prefix = prefix});
        return response.S3Objects.Select(s => s.Key).ToList();
    }

    private async Task<List<string>> GetS3KeysAfter(string prefix, string startAfter)
    {
        var response = await _s3.ListObjectsV2Async(
                           new ListObjectsV2Request {BucketName = _settings.Bucket, Prefix = prefix, StartAfter = startAfter}
                       );
        return response.S3Objects.Select(s => s.Key).ToList();
    }

    private static string JoinPath(string prefix, string suffix)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            prefix = prefix.TrimEnd('/');
            parts.Add(prefix);
        }

        if (!string.IsNullOrWhiteSpace(suffix))
        {
            suffix = suffix.TrimStart('/');
            parts.Add(suffix);
        }

        return string.Join("/", parts);
    }

    private async Task<List<TrackSpotifyPopularityInfo>> LoadIndex(string key)
    {
        var content = await _s3.GetObjectAsync(_settings.Bucket, key);
        using var reader = new StreamReader(content.ResponseStream);
        var csvContent = await reader.ReadToEndAsync();

        using var sr = new StringReader(csvContent);
        using var csv = new CsvReader(sr, CultureInfo.InvariantCulture);

        var result = new List<TrackSpotifyPopularityInfo>();

        await csv.ReadAsync();
        csv.ReadHeader();

        if (!(csv.HeaderRecord?.Contains("ISRC") ?? false))
        {
            _log.LogWarning("File {} is in old format and doesn't have ISRC, skipping", key);
            return result;
        }

        while (await csv.ReadAsync())
        {
            var record = new TrackSpotifyPopularityInfo {Isrc = csv.GetField<string>("ISRC"), Popularity = csv.GetField<int>("Popularity")};

            result.Add(record);
        }


        return result;
    }

    private async Task<bool> NeedToLoadFullIndex()
    {
        return await _mainDb.ExternalSongs.CountAsync(s => s.SpotifyPopularity != null) < 10;
    }

    private async Task UpdatePopularity(IEnumerable<TrackSpotifyPopularityInfo> popularity)
    {
        var batches = popularity.Select((a, i) => new {Element = a, Index = i})
                                .GroupBy(a => a.Index / UpdateSpotifyPopularityInDbBatchSize)
                                .Select(g => g.Select(a => a.Element).ToArray())
                                .ToArray();

        foreach (var batch in batches)
        {
            var values = string.Join(",", batch.Select(a => $"('{a.Isrc}', {a.Popularity})"));

            var sql = $@"
with spotify as
         (select *
          from (values 
                    {values}
                ) as t (isrc, pop))
update ""ExternalSong"" src
    set ""SpotifyPopularity"" = spotify.pop,
        ""SpotifyPopularityLastUpdate"" = current_timestamp
from ""ExternalSong"" es join spotify on es.""Isrc"" = spotify.isrc
where src.""ExternalTrackId"" = es.""ExternalTrackId""
";
            await _mainDb.ExecuteSqlRawAsync(sql);
        }
    }
}

public class TrackSpotifyPopularityInfo
{
    public string Isrc { get; set; }

    public int Popularity { get; set; }
}