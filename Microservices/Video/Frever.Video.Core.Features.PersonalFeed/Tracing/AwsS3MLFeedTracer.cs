using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Frever.Video.Core.Features.PersonalFeed.Tracing;

public class AwsS3MLFeedTracer : IMLFeedTracer
{
    private static readonly string ExportPath = "video-feeds/fyp-v2/";

    private static readonly JsonSerializerSettings JsonSerializerSettings =
        new() {ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.Indented};

    private readonly string _bucketName;

    private readonly MLFeedBuildingInfo _info;
    private readonly IAmazonS3 _s3;

    public AwsS3MLFeedTracer(long groupId, string bucketName, IAmazonS3 s3)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(bucketName));

        _bucketName = bucketName;
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));

        _info = new MLFeedBuildingInfo {GroupId = groupId};
    }

    public async ValueTask DisposeAsync()
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");
        _info.Timestamp = timestamp;

        var json = JsonConvert.SerializeObject(_info, JsonSerializerSettings);
        var key = $"{ExportPath}{_info.GroupId}_{timestamp}_{Guid.NewGuid()}.json";


        await _s3.PutObjectAsync(new PutObjectRequest {BucketName = _bucketName, ContentBody = json, Key = key});
    }

    public Task TraceViews(ISet<long> views)
    {
        ArgumentNullException.ThrowIfNull(views);

        _info.Views = views.OrderBy(v => v).ToHashSet();

        return Task.CompletedTask;
    }

    public Task TraceOriginalMLServerResponse(MLVideoRef[] videos)
    {
        ArgumentNullException.ThrowIfNull(videos);

        _info.OriginalMlServerResponse = videos;
        return Task.CompletedTask;
    }

    public Task TraceViewedVideoIds(long[] viewedVideoIds)
    {
        ArgumentNullException.ThrowIfNull(viewedVideoIds);

        _info.ViewedVideoIds = viewedVideoIds;

        return Task.CompletedTask;
    }

    public Task TraceNotFromGeoClusterVideos(long[] videoIds)
    {
        ArgumentNullException.ThrowIfNull(videoIds);

        _info.NotFromGeoClusterVideoIds = videoIds;

        return Task.CompletedTask;
    }

    public Task TraceGeoClusters(long[] geoClusterIds)
    {
        _info.GeoClusterIds = geoClusterIds;
        return Task.CompletedTask;
    }

    public Task TraceNotInMemoryVideoCache(long[] videoIds)
    {
        ArgumentNullException.ThrowIfNull(videoIds);

        _info.VideosNotInMemoryCache = videoIds;

        return Task.CompletedTask;
    }

    public Task TraceResults(MLVideoRef[] results)
    {
        _info.Results = results ?? throw new ArgumentNullException(nameof(results));
        return Task.CompletedTask;
    }
}

public interface IMLFeedTracer : IAsyncDisposable
{
    Task TraceViews(ISet<long> views);
    Task TraceOriginalMLServerResponse(MLVideoRef[] videos);
    Task TraceViewedVideoIds(long[] viewedVideoIds);
    Task TraceNotFromGeoClusterVideos(long[] videoIds);
    Task TraceGeoClusters(long[] geoClusterIds);
    Task TraceNotInMemoryVideoCache(long[] videoIds);
    Task TraceResults(MLVideoRef[] results);
}

public class MLFeedBuildingInfo
{
    public string Timestamp { get; set; }

    public long GroupId { get; set; }

    public long[] GeoClusterIds { get; set; }

    public ISet<long> Views { get; set; }

    public MLVideoRef[] OriginalMlServerResponse { get; set; }

    public long[] ViewedVideoIds { get; set; }

    public long[] NotFromGeoClusterVideoIds { get; set; }

    public MLVideoRef[] Results { get; set; }

    public long[] VideosNotInMemoryCache { get; set; }
}