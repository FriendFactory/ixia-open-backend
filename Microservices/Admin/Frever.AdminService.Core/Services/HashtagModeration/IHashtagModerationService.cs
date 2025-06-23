using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Frever.AdminService.Core.Services.HashtagModeration.Contracts;
using Newtonsoft.Json.Linq;

namespace Frever.AdminService.Core.Services.HashtagModeration;

public interface IHashtagModerationService
{
    Task<bool> SoftDeleteAsync(long hashtagId, CancellationToken token = default);

    Task<HashtagInfo> UpdateByIdAsync(long hashtagId, JObject hashtagEdit, CancellationToken token = default);

    Task<IReadOnlyList<HashtagInfo>> GetAll(GetHashtagsRequest hashtagsRequest);
}