using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Frever.Video.Contract;

namespace Frever.Video.Core.Features.Hashtags;

public interface IHashtagService
{
    Task<IReadOnlyList<HashtagInfo>> GetHashtagListAsync(HashtagRequest requestOptions, CancellationToken token = default);
    Task<HashtagInfo[]> GetHashtagByIds(long[] hashtagIds);
    Task<long[]> GetTrendingHashtagIds(int take);
}