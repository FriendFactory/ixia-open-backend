using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Frever.Cache;
using Frever.Client.Core.Features.Sounds.FavoriteSounds;
using Frever.Client.Core.Features.Sounds.Song.DataAccess;
using Frever.Client.Core.Utils;
using Frever.Client.Shared.Files;
using Frever.ClientService.Contract.Sounds;
using Frever.Shared.MainDb.Entities;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.EntityFrameworkCore;
using Genre = Frever.Client.Core.Features.Sounds.Song.Models.Genre;
using SongInfo = Frever.ClientService.Contract.Sounds.SongInfo;

namespace Frever.Client.Core.Features.Sounds.Song;

internal sealed class SongAssetService : ISongAssetService
{
    public const int SongCommercialLabel = 4;
    private static readonly string SongAssetCacheKey = $"{nameof(Models.Song)}".FreverAssetCacheKey();
    private static readonly string PromotedSongCacheKey = $"{nameof(PromotedSong)}".FreverAssetCacheKey();

    private readonly IAssetServerSettings _assetServerSettings;
    private readonly ICurrentLocationProvider _currentLocationProvider;
    private readonly UserInfo _currentUser;
    private readonly IFavoriteSoundRepository _favoriteSoundRepo;
    private readonly IBlobCache<Genre[]> _genreCache;
    private readonly IMapper _mapper;
    private readonly IMusicGeoFilter _musicGeoFilter;
    private readonly IBlobCache<PromotedSongDto[]> _promotedSongCache;
    private readonly ISongAssetRepository _repo;
    private readonly IBlobCache<Models.Song[]> _songListCache;
    private readonly IUserPermissionService _userPermissionService;
    private readonly IFileStorageService _fileStorageService;

    public SongAssetService(
        ISongAssetRepository repo,
        IBlobCache<Genre[]> genreCache,
        IBlobCache<Models.Song[]> songListCache,
        IBlobCache<PromotedSongDto[]> promotedSongCache,
        IMapper mapper,
        IUserPermissionService userPermissionService,
        IAssetServerSettings assetServerSettings,
        UserInfo currentUser,
        ICurrentLocationProvider currentLocationProvider,
        IFavoriteSoundRepository favoriteSoundRepo,
        IMusicGeoFilter musicGeoFilter,
        IFileStorageService fileStorageService
    )
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _genreCache = genreCache ?? throw new ArgumentNullException(nameof(genreCache));
        _songListCache = songListCache ?? throw new ArgumentNullException(nameof(songListCache));
        _promotedSongCache = promotedSongCache ?? throw new ArgumentNullException(nameof(promotedSongCache));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _userPermissionService = userPermissionService ?? throw new ArgumentNullException(nameof(userPermissionService));
        _assetServerSettings = assetServerSettings ?? throw new ArgumentNullException(nameof(assetServerSettings));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _currentLocationProvider = currentLocationProvider;
        _favoriteSoundRepo = favoriteSoundRepo ?? throw new ArgumentNullException(nameof(favoriteSoundRepo));
        _musicGeoFilter = musicGeoFilter ?? throw new ArgumentNullException(nameof(musicGeoFilter));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
    }

    public async Task<SongInfo[]> GetSongListAsync(SongFilterModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        await _userPermissionService.EnsureCurrentUserActive();

        var all = await GetCachedSongs();

        var filtered = (await GetFiltered(model, all)).Skip(model.Skip).Take(model.Take);

        var result = _mapper.Map<SongInfo[]>(filtered, options => options.AddNewAssetDays(_assetServerSettings.NewAssetDays));

        var ids = await _favoriteSoundRepo.GetFavoriteSongIds(_currentUser, result.Select(e => e.Id));

        foreach (var item in result)
            item.IsFavorite = ids.Contains(item.Id);

        await _fileStorageService.InitUrls<Frever.Shared.MainDb.Entities.Song>(result);

        return result;
    }

    public async Task<SongInfo> GetSongById(long id)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var song = await _repo.GetSongs()
                              .ReadyForUserRole(_currentUser)
                              .Select(
                                   e => new SongInfo
                                        {
                                            Id = e.Id,
                                            GenreId = e.GenreId,
                                            Name = e.Name,
                                            Duration = e.Duration,
                                            UsageCount = e.UsageCount,
                                            Files = e.Files,
                                            Artist = new ArtistInfo {Id = e.Artist.Id, Name = e.Artist.Name},
                                            Album = e.AlbumId == null
                                                        ? null
                                                        : new AlbumInfo {Id = e.Album.Id, Name = e.Album.Name}
                                        }
                               )
                              .FirstOrDefaultAsync(e => e.Id == id);

        if (song is not null)
            song.IsFavorite = await _favoriteSoundRepo.GetSoundsByGroupId(_currentUser).AnyAsync(s => s.SongId == id);

        await _fileStorageService.InitUrls<Frever.Shared.MainDb.Entities.Song>(song);

        return song;
    }

    public async Task<ExternalSongDto> GetExternalSongById(long id)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var externalSong = await _repo.GetAvailableExternalSongIds(id).Select(e => new {e.Id, e.UsageCount}).FirstOrDefaultAsync();

        return externalSong is null
                   ? null
                   : new ExternalSongDto
                     {
                         Id = externalSong.Id,
                         UsageCount = externalSong.UsageCount,
                         IsAvailable = true,
                         IsFavorite = _favoriteSoundRepo.GetSoundsByGroupId(_currentUser).Any(s => s.ExternalSongId == id)
                     };
    }

    public async Task<ExternalSongDto[]> GetAvailableExternalSongs(long[] ids)
    {
        if (ids == null || ids.Length == 0)
            return [];

        await _userPermissionService.EnsureCurrentUserActive();

        var externalSongs = await _repo.GetAvailableExternalSongIds(ids)
                                       .Select(e => new {e.Id, e.UsageCount})
                                       .ToDictionaryAsync(e => e.Id, e => e.UsageCount);

        var favoriteIds = await _favoriteSoundRepo.GetFavoriteExternalSongIds(_currentUser, ids);

        return ids.Distinct()
                  .Select(
                       e => new ExternalSongDto
                            {
                                Id = e,
                                IsAvailable = externalSongs.ContainsKey(e),
                                IsFavorite = favoriteIds.Contains(e),
                                UsageCount = externalSongs.GetValueOrDefault(e)
                            }
                   )
                  .ToArray();
    }

    public async Task<PromotedSongDto[]> GetPromotedSongs(int skip, int take)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var all = await _promotedSongCache.GetOrCache(PromotedSongCacheKey, PromotedSongs, TimeSpan.FromDays(1));

        all = await _musicGeoFilter.FilterOutUnavailableSongs(
                  (await _currentLocationProvider.Get()).CountryIso3Code,
                  all,
                  a => a.ExternalSongId == null ? [] : [a.ExternalSongId.Value],
                  a => a.SongId == null ? [] : [a.SongId.Value]
              );

        var result = all.Skip(skip).Take(take).ToArray();

        await _fileStorageService.InitUrls<PromotedSong>(result);

        return result;

        Task<PromotedSongDto[]> PromotedSongs()
        {
            return _repo.GetPromotedSongs()
                        .OrderBy(e => e.SortOrder)
                        .ThenBy(e => e.Id)
                        .ProjectTo<PromotedSongDto>(_mapper.ConfigurationProvider)
                        .ToArrayAsync();
        }
    }

    public async Task<Models.Song[]> GetSongs(long[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);

        await _userPermissionService.EnsureCurrentUserActive();

        var all = await GetCachedSongs();

        var result = all.ReadyForUserRole(_currentUser).Where(a => ids.Contains(a.Id)).ToArray();

        await _fileStorageService.InitUrls<Frever.Shared.MainDb.Entities.Song>(result);

        return result;
    }

    public async Task<Genre[]> GetAvailableGenres(string country = null)
    {
        var all = await _genreCache.GetOrCache(nameof(Genre).FreverAssetCacheKey(), ReadGenresFromDb, TimeSpan.FromDays(1));

        country ??= (await _currentLocationProvider.Get()).CountryIso3Code;

        return all.Where(e => e.Countries == null || e.Countries.Contains(country)).ToArray();

        Task<Genre[]> ReadGenresFromDb()
        {
            return _repo.GetGenres().AsNoTracking().ProjectTo<Genre>(_mapper.ConfigurationProvider).ToArrayAsync();
        }
    }

    private async Task<Models.Song[]> GetCachedSongs()
    {
        var all = await _songListCache.GetOrCache(SongAssetCacheKey, ReadSongsFromDb, TimeSpan.FromDays(1));

        var location = (await _currentLocationProvider.Get()).CountryIso3Code;
        location = location.ToLowerInvariant();

        return all.Where(e => e.AvailableForCountries.Length == 0 || e.AvailableForCountries.Contains(location)).ToArray();

        Task<Models.Song[]> ReadSongsFromDb()
        {
            return _repo.GetSongs().OrderByDescending(e => e.Id).ProjectTo<Models.Song>(_mapper.ConfigurationProvider).ToArrayAsync();
        }
    }

    private async Task<Models.Song[]> GetFiltered(SongFilterModel model, IEnumerable<Models.Song> all)
    {
        var result = all.ReadyForUserRole(_currentUser).AvailableForUserRole(_currentUser);

        if (!string.IsNullOrWhiteSpace(model.Name))
            result = result.Where(
                e => e.Name.StartsWith(model.Name, StringComparison.OrdinalIgnoreCase) || e.Artist.Name.StartsWith(
                         model.Name,
                         StringComparison.OrdinalIgnoreCase
                     )
            );

        if (model.CommercialOnly is true)
            result = result.Where(e => e.LabelId == SongCommercialLabel);

        if (model.GenreId.HasValue)
            result = result.Where(e => e.GenreId == model.GenreId);

        if (model.Ids is {Length: > 0})
            result = result.Where(song => model.Ids.Contains(song.Id));

        var genres = await GetAvailableGenres();

        return result.Where(e => genres.Select(e => e.Id).Contains(e.GenreId)).ToArray();
    }
}

public class SongFileConfig : DefaultFileMetadataConfiguration<Frever.Shared.MainDb.Entities.Song>
{
    public SongFileConfig()
    {
        AddMainFile("mp3");
        AddThumbnail(128, "png");
        AddThumbnail(256, "png");
        AddThumbnail(512, "png");
    }
}

public class PromotedSongDtoFileConfig : DefaultFileMetadataConfiguration<PromotedSong>
{
    public PromotedSongDtoFileConfig()
    {
        AddThumbnail(512, "png");
    }
}