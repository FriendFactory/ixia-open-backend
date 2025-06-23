using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AuthServerShared;
using Frever.Cache;
using Frever.Client.Core.Features.Sounds.FavoriteSounds;
using Frever.Client.Core.Features.Sounds.UserSounds.DataAccess;
using Frever.Client.Shared.Files;
using Frever.Client.Shared.Social.Services;
using Frever.ClientService.Contract.Social;
using Frever.ClientService.Contract.Sounds;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.Sounds.UserSounds.Trending;

public class TrendingUserSoundService(
    UserInfo currentUser,
    IListCache<UserSoundFullInfo> cache,
    IUserSoundAssetRepository userSoundRepo,
    IFavoriteSoundRepository repo,
    ISocialSharedService socialSharedService,
    IFileStorageService fileStorageService
) : ITrendingUserSoundService
{
    private readonly string _cacheKey = "user-sound::trending".FreverAssetCacheKey();

    public async Task<List<UserSoundFullInfo>> GetTrendingUserSound(string filter, int skip, int take)
    {
        var result = string.IsNullOrWhiteSpace(filter)
                         ? await cache.GetOrCache(
                               _cacheKey,
                               ReadPageFromDb,
                               skip,
                               take,
                               TimeSpan.FromHours(3)
                           )
                         : await ReadPageFromDb(filter, skip, Math.Clamp(take, 0, 20));

        var ids = await repo.GetFavoriteUserSoundIds(currentUser, result.Select(e => e.Id));
        var ownerIds = result.Select(e => e.Owner.Id).ToArray();
        var groupInfo = await socialSharedService.GetGroupShortInfo(ownerIds);

        foreach (var item in result)
        {
            item.IsFavorite = ids.Contains(item.Id);
            item.Owner = groupInfo.GetValueOrDefault(item.Owner.Id);
        }

        await fileStorageService.InitUrls<UserSound>(result);

        return result;
    }

    private Task<UserSoundFullInfo[]> ReadPageFromDb(int skip, int take)
    {
        return userSoundRepo.GetTrendingUserSound().Skip(skip).Take(take).Select(Selector()).ToArrayAsync();
    }

    private Task<List<UserSoundFullInfo>> ReadPageFromDb(string filter, int skip, int take)
    {
        return userSoundRepo.GetTrendingUserSound()
                            .Where(e => e.Name.StartsWith(filter))
                            .Skip(skip)
                            .Take(take)
                            .Select(Selector())
                            .ToListAsync();
    }

    private static Expression<Func<UserSound, UserSoundFullInfo>> Selector()
    {
        return e => new UserSoundFullInfo
                    {
                        Id = e.Id,
                        Name = e.Name,
                        Duration = e.Duration,
                        CreatedTime = e.CreatedTime,
                        UsageCount = e.UsageCount,
                        Files = e.Files,
                        Owner = new GroupShortInfo {Id = e.GroupId}
                    };
    }
}