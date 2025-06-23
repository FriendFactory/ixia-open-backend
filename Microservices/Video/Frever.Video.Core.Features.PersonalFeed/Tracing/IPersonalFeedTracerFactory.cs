using System.Threading.Tasks;

namespace Frever.Video.Core.Features.PersonalFeed.Tracing;

public interface IPersonalFeedTracerFactory
{
    Task<IPersonalFeedTracer> StartMixingEngineTracing(long groupId);

    Task<IMLFeedTracer> StartMLFeedTracing(long groupId);
}