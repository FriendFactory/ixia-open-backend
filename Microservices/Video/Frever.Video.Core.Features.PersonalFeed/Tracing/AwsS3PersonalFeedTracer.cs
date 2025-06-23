using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Frever.Video.Core.Features.PersonalFeed.Tracing;

public class AwsS3PersonalFeedTracer : IPersonalFeedTracer
{
    private static readonly string ExportPath = "video-feeds/fyp/";

    private static readonly JsonSerializerSettings JsonSerializerSettings =
        new() {ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.Indented};

    private readonly string _bucketName;

    private readonly PersonalFeedBuildingInfo _info;
    private readonly IAmazonS3 _s3;

    public AwsS3PersonalFeedTracer(long groupId, string bucketName, IAmazonS3 s3)
    {
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(bucketName));

        _bucketName = bucketName;
        _s3 = s3 ?? throw new ArgumentNullException(nameof(s3));

        _info = new PersonalFeedBuildingInfo {GroupId = groupId};
    }

    public async ValueTask DisposeAsync()
    {
        var json = JsonConvert.SerializeObject(_info, JsonSerializerSettings);
        var key = $"{ExportPath}{_info.GroupId}_{DateTime.Now:yyyy-MM-ddTHH-mm-ss}_{Guid.NewGuid()}.json";


        await _s3.PutObjectAsync(new PutObjectRequest {BucketName = _bucketName, ContentBody = json, Key = key});
    }

    public Task TraceViews(ISet<long> views)
    {
        ArgumentNullException.ThrowIfNull(views);

        _info.Views = views.OrderBy(v => v).ToHashSet();

        return Task.CompletedTask;
    }

    public Task TracePriorityMixingSource(NormalizedSourceInfo source)
    {
        ArgumentNullException.ThrowIfNull(source);

        _info.Sources = (_info.Sources ?? Enumerable.Empty<NormalizedSourceInfo>()).Append(source).ToArray();
        return Task.CompletedTask;
    }

    public Task TraceNormalizedMixingSources(NormalizedSourceInfo[] sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        _info.Sources = (_info.Sources ?? Enumerable.Empty<NormalizedSourceInfo>()).Concat(sources).ToArray();
        return Task.CompletedTask;
    }

    public Task TraceResults(PersonalFeedVideo[] result)
    {
        ArgumentNullException.ThrowIfNull(result);

        _info.Results = result;
        return Task.CompletedTask;
    }
}

public class PersonalFeedBuildingInfo
{
    public long GroupId { get; set; }

    public ISet<long> Views { get; set; }

    public NormalizedSourceInfo[] Sources { get; set; }

    public PersonalFeedVideo[] Results { get; set; }
}