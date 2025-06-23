using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure.Sounds;
using Common.Infrastructure.Utils;
using Common.Models.Files;
using Frever.Client.Core.Features.Sounds.Song;
using Frever.Client.Core.Features.Sounds.Song.DataAccess;
using Frever.Client.Core.Utils;
using Frever.Client.Shared.Files;
using Frever.Client.Shared.Social.Services;
using Frever.ClientService.Contract.Sounds;
using Frever.Shared.MainDb.Entities;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.Sounds.FavoriteSounds;

public interface IFavoriteSoundService
{
    Task<FavoriteSoundDto[]> GetMyFavoriteSounds(bool? commercialOnly, int skip, int take);
    Task<FavoriteSoundDto> AddFavoriteSound(long id, FavoriteSoundType type);
    Task RemoveFavoriteSound(long id, FavoriteSoundType type);
}

internal sealed class FavoriteSoundService : IFavoriteSoundService
{
    private readonly UserInfo _currentUser;
    private readonly ICurrentLocationProvider _location;
    private readonly ILogger _log;
    private readonly IMusicGeoFilter _musicFilter;
    private readonly ISocialSharedService _socialSharedService;
    private readonly ISongAssetRepository _songRepo;
    private readonly IFavoriteSoundRepository _soundRepo;
    private readonly IUserPermissionService _userPermissionService;
    private readonly IFileStorageService _fileStorageService;

    public FavoriteSoundService(
        IFavoriteSoundRepository soundRepo,
        ISongAssetRepository songRepo,
        IUserPermissionService userPermissionService,
        ISocialSharedService socialSharedService,
        UserInfo currentUser,
        ICurrentLocationProvider location,
        IMusicGeoFilter musicFilter,
        ILoggerFactory loggerFactory,
        IFileStorageService fileStorageService
    )
    {
        _soundRepo = soundRepo ?? throw new ArgumentNullException(nameof(soundRepo));
        _songRepo = songRepo ?? throw new ArgumentNullException(nameof(songRepo));
        _userPermissionService = userPermissionService ?? throw new ArgumentNullException(nameof(userPermissionService));
        _socialSharedService = socialSharedService ?? throw new ArgumentNullException(nameof(socialSharedService));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _location = location ?? throw new ArgumentNullException(nameof(location));
        _musicFilter = musicFilter ?? throw new ArgumentNullException(nameof(musicFilter));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _log = loggerFactory.CreateLogger("Frever.Client.FavoriteSounds");
    }

    public async Task<FavoriteSoundDto[]> GetMyFavoriteSounds(bool? commercialOnly, int skip, int take)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        if (commercialOnly is true)
            return await GetCommercialSongs(skip, take);

        var sounds = await CollectionLoader.LoadFiltered<FavoriteSoundInternal>(
                         take,
                         async (loaded, takeRest) =>
                         {
                             var result = await _soundRepo.GetFavoriteSoundInternalQuery(_currentUser)
                                                          .OrderByDescending(a => a.Time)
                                                          .Skip(skip + loaded.Count)
                                                          .Take(takeRest)
                                                          .ToListAsync();
                             return result;
                         },
                         async collection =>
                         {
                             var result = await _musicFilter.FilterOutUnavailableSongs(
                                              (await _location.Get()).CountryIso3Code,
                                              collection,
                                              s => s.Type == (int)FavoriteSoundType.ExternalSong ? [s.Id] : null,
                                              s => s.Type == (int)FavoriteSoundType.Song ? [s.Id] : null
                                          );
                             return result;
                         },
                         _log
                     );

        var result = await ToFavoriteSoundDto(sounds);

        return result;
    }

    public async Task<FavoriteSoundDto> AddFavoriteSound(long id, FavoriteSoundType type)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var exist = await _soundRepo.GetSoundsByGroupId(_currentUser).Where(GetByType(type, id)).AnyAsync();
        if (!exist)
        {
            var newSound = type switch
            {
                FavoriteSoundType.Song => new FavoriteSound { SongId = id },
                FavoriteSoundType.ExternalSong => new FavoriteSound { ExternalSongId = id },
                FavoriteSoundType.UserSound => new FavoriteSound { UserSoundId = id },
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            newSound.GroupId = _currentUser.UserMainGroupId;
            await _soundRepo.AddFavoriteSound(newSound);
        }

        var sound = await _soundRepo.GetFavoriteSoundInternalQueryById(id, type, _currentUser).FirstOrDefaultAsync();
        var result = await ToFavoriteSoundDto(sound);

        return result.FirstOrDefault();
    }

    public async Task RemoveFavoriteSound(long id, FavoriteSoundType type)
    {
        var sound = await _soundRepo.GetSoundsByGroupId(_currentUser).Where(GetByType(type, id)).FirstOrDefaultAsync();
        if (sound == null)
            return;

        await _soundRepo.RemoveFavoriteSound(sound);
    }

    private async Task<FavoriteSoundDto[]> GetCommercialSongs(int skip, int take)
    {
        var result = await _soundRepo.GetSoundsByGroupId(_currentUser)
                         .Join(
                              _soundRepo.GetSongByLabelId(SongAssetService.SongCommercialLabel).ReadyForUserRole(_currentUser),
                              snd => snd.SongId,
                              s => s.Id,
                              (snd, s) => new FavoriteSoundDto
                              {
                                  Id = s.Id,
                                  SongName = s.Name,
                                  ArtistName = s.Artist.Name,
                                  Duration = s.Duration,
                                  Files = s.Files,
                                  Type = FavoriteSoundType.Song
                              }
                          )
                         .Skip(skip)
                         .Take(take)
                         .ToArrayAsync();

        await _fileStorageService.InitUrls<Frever.Shared.MainDb.Entities.Song>(result);

        return result;
    }

    private async Task<FavoriteSoundDto[]> GetCommercialSongsV2(DateTime before, int takeNext)
    {
        var result = await _soundRepo.GetSoundsByGroupId(_currentUser)
                                     .Join(
                                          _songRepo.GetSongs()
                                                   .Where(e => e.LabelId == SongAssetService.SongCommercialLabel)
                                                   .ReadyForUserRole(_currentUser),
                                          snd => snd.SongId,
                                          s => s.Id,
                                          (snd, s) => new { Sound = snd, Song = s }
                                      )
                                     .Where(a => a.Sound.Time < before)
                                     .OrderByDescending(a => a.Sound.Time)
                                     .Take(takeNext)
                                     .Select(
                                          a => new FavoriteSoundDto
                                          {
                                              Id = a.Song.Id,
                                              SongName = a.Song.Name,
                                              ArtistName = a.Song.Artist.Name,
                                              Duration = a.Song.Duration,
                                              Files = a.Song.Files,
                                              Type = FavoriteSoundType.Song,
                                              Key = a.Sound.Time.ToString("O")
                                          }
                                      )
                                     .ToArrayAsync();

        await _fileStorageService.InitUrls<UserSound>(result);

        return result;
    }

    private static Expression<Func<FavoriteSound, bool>> GetByType(FavoriteSoundType type, long id)
    {
        return type switch
        {
            FavoriteSoundType.Song => e => e.SongId == id,
            FavoriteSoundType.ExternalSong => e => e.ExternalSongId == id,
            FavoriteSoundType.UserSound => e => e.UserSoundId == id,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private async Task<FavoriteSoundDto[]> ToFavoriteSoundDto(params FavoriteSoundInternal[] sounds)
    {
        if (sounds is null || sounds.Length == 0)
            return [];

        var groupIds = sounds.Where(e => e.OwnerGroupId.HasValue).Select(e => e.OwnerGroupId.Value).ToArray();

        var groupInfo = await _socialSharedService.GetGroupShortInfo(groupIds);

        var result = sounds.Select(
                                e => new FavoriteSoundDto
                                {
                                    Id = e.Id,
                                    Type = (FavoriteSoundType)e.Type,
                                    SongName = e.SongName,
                                    ArtistName = e.ArtistName,
                                    Duration = e.Duration,
                                    UsageCount = e.UsageCount,
                                    Owner = groupInfo.GetValueOrDefault(e.OwnerGroupId ?? 0),
                                    Key = e.Time.ToString("O", CultureInfo.InvariantCulture),
                                    Files = e.Files == null ? null : JsonConvert.DeserializeObject<FileMetadata[]>(e.Files)
                                }
                            )
                           .ToArray();

        await _fileStorageService.InitUrls<Frever.Shared.MainDb.Entities.Song>(result.Where(e => e.Type == FavoriteSoundType.Song));
        await _fileStorageService.InitUrls<UserSound>(result.Where(e => e.Type == FavoriteSoundType.UserSound));

        return result;
    }
}