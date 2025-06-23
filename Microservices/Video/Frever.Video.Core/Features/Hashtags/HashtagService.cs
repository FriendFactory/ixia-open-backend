using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using Frever.Video.Contract;
using Frever.Video.Core.Features.Hashtags.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.Hashtags;

internal sealed class HashtagService(IHashtagRepository repo, IUserPermissionService userPermissionService) : IHashtagService
{
    public async Task<IReadOnlyList<HashtagInfo>> GetHashtagListAsync(HashtagRequest requestOptions, CancellationToken token = default)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var source = repo.GetAllAsNoTracking().Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(requestOptions.Name))
            source = source.Where(e => e.Name.ToLower().StartsWith(requestOptions.Name.ToLower()));

        var result = await source.OrderByDescending(e => e.ViewsCount)
                                 .Skip(requestOptions.Skip ?? 0)
                                 .Take(requestOptions.Take)
                                 .Select(
                                      e => new HashtagInfo
                                           {
                                               Id = e.Id,
                                               Name = e.Name,
                                               ViewsCount = e.ViewsCount,
                                               UsageCount = e.VideoCount
                                           }
                                  )
                                 .ToListAsync(token);

        return result;
    }

    public async Task<HashtagInfo[]> GetHashtagByIds(long[] hashtagIds)
    {
        if (hashtagIds is null || hashtagIds.Length == 0)
            return null;

        var hashtags = await repo.GetHashtagByIds(hashtagIds);

        return hashtags.Select(
                            e => new HashtagInfo
                                 {
                                     Id = e.Id,
                                     Name = e.Name,
                                     ViewsCount = e.ViewsCount,
                                     UsageCount = e.VideoCount,
                                     Key = e.Id.ToString()
                                 }
                        )
                       .ToArray();
    }

    public Task<long[]> GetTrendingHashtagIds(int take)
    {
        return repo.GetTrendingHashtagIds(take);
    }
}