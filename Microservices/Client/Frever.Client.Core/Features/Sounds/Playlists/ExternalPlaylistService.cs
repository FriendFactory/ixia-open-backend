using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Frever.Cache;
using Frever.Client.Core.Utils;
using Frever.ClientService.Contract.Sounds;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.Sounds.Playlists;

internal sealed class ExternalPlaylistService : IExternalPlaylistService
{
    private readonly UserInfo _currentUser;
    private readonly ICurrentLocationProvider _locationProvider;
    private readonly IMapper _mapper;
    private readonly IBlobCache<ExternalPlaylistModel[]> _playlistCache;
    private readonly IExternalPlaylistRepository _repo;
    private readonly IUserPermissionService _userPermissionService;

    public ExternalPlaylistService(
        IExternalPlaylistRepository repo,
        UserInfo currentUser,
        IMapper mapper,
        IBlobCache<ExternalPlaylistModel[]> playlistCache,
        IUserPermissionService userPermissionService,
        ICurrentLocationProvider locationProvider
    )
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _playlistCache = playlistCache ?? throw new ArgumentNullException(nameof(playlistCache));
        _userPermissionService = userPermissionService ?? throw new ArgumentNullException(nameof(userPermissionService));
        _locationProvider = locationProvider ?? throw new ArgumentNullException(nameof(locationProvider));
    }

    public async Task<ExternalPlaylistInfo[]> GetPlaylists(ExternalPlaylistFilterModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        await _userPermissionService.EnsureCurrentUserActive();

        var all = await GetExternalPlaylists();

        var location = (await _locationProvider.Get()).CountryIso3Code;

        var filtered = all.ReadyForUserRole(_currentUser)
                          .Where(e => e.Countries == null || e.Countries.Contains(location))
                          .TakePage(model.Target, model.TakePrevious, model.TakeNext, e => e.Id)
                          .ToArray();

        return _mapper.Map<ExternalPlaylistInfo[]>(filtered);
    }

    public async Task<ExternalPlaylistInfo> GetById(long id)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var all = await GetExternalPlaylists();

        var externalPlaylist = all.FirstOrDefault(e => e.Id == id);
        if (externalPlaylist is null || !externalPlaylist.ReadyForUserRole(_currentUser))
            return null;

        if (externalPlaylist.Countries == null)
            return _mapper.Map<ExternalPlaylistInfo>(externalPlaylist);

        var country = (await _locationProvider.Get()).CountryIso3Code;

        return externalPlaylist.Countries.Contains(country) ? _mapper.Map<ExternalPlaylistInfo>(externalPlaylist) : null;
    }

    private Task<ExternalPlaylistModel[]> GetExternalPlaylists()
    {
        return _playlistCache.GetOrCache(nameof(ExternalPlaylistModel).FreverAssetCacheKey(), ReadPlaylistsFromDb, TimeSpan.FromDays(3));

        Task<ExternalPlaylistModel[]> ReadPlaylistsFromDb()
        {
            return _repo.GetPlaylists().OrderBy(e => e.SortOrder).ProjectTo<ExternalPlaylistModel>(_mapper.ConfigurationProvider).ToArrayAsync();
        }
    }
}