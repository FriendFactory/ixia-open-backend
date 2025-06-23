using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frever.Video.Core.Features.PersonalFeed.Tracing;

public class StubPersonalFeedTracerFactory : IPersonalFeedTracerFactory
{
    public Task<IPersonalFeedTracer> StartMixingEngineTracing(long groupId)
    {
        return Task.FromResult<IPersonalFeedTracer>(new StubPersonalFeedTracer());
    }

    public Task<IMLFeedTracer> StartMLFeedTracing(long groupId)
    {
        return Task.FromResult<IMLFeedTracer>(new StubPersonalFeedTracer());
    }
}

public class StubPersonalFeedTracer : IPersonalFeedTracer, IMLFeedTracer
{
    public Task TraceOriginalMLServerResponse(MLVideoRef[] videos)
    {
        return Task.CompletedTask;
    }

    public Task TraceViewedVideoIds(long[] viewedVideoIds)
    {
        return Task.CompletedTask;
    }

    public Task TraceNotFromGeoClusterVideos(long[] videoIds)
    {
        return Task.CompletedTask;
    }

    public Task TraceGeoClusters(long[] geoClusterIds)
    {
        return Task.CompletedTask;
    }

    public Task TraceNotInMemoryVideoCache(long[] videoIds)
    {
        return Task.CompletedTask;
    }

    public Task TraceResults(MLVideoRef[] results)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return default;
    }

    public Task TraceViews(ISet<long> views)
    {
        if (views == null)
            throw new ArgumentNullException(nameof(views));
        return Task.CompletedTask;
    }

    public Task TracePriorityMixingSource(NormalizedSourceInfo source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        return Task.CompletedTask;
    }

    public Task TraceNormalizedMixingSources(NormalizedSourceInfo[] sources)
    {
        if (sources == null)
            throw new ArgumentNullException(nameof(sources));
        return Task.CompletedTask;
    }

    public Task TraceResults(PersonalFeedVideo[] result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));
        return Task.CompletedTask;
    }
}