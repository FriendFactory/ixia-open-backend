using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frever.Video.Core.Features.PersonalFeed.Tracing;

public interface IPersonalFeedTracer : IAsyncDisposable
{
    Task TraceViews(ISet<long> views);

    Task TracePriorityMixingSource(NormalizedSourceInfo source);

    Task TraceNormalizedMixingSources(NormalizedSourceInfo[] sources);

    Task TraceResults(PersonalFeedVideo[] result);
}