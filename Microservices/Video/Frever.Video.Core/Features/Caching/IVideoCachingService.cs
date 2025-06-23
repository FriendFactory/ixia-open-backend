using System.Threading.Tasks;

namespace Frever.Video.Core.Features.Caching;

public interface IVideoCachingService
{
    Task DeleteVideoDetailsCache(long videoId);
}