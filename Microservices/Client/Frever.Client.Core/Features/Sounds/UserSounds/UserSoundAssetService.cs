using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServer.Permissions.Sub13;
using AuthServerShared;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Infrastructure;
using Common.Infrastructure.Caching;
using Common.Infrastructure.Caching.CacheKeys;
using FluentValidation;
using Frever.Cache;
using Frever.Client.Core.Features.MediaFingerprinting;
using Frever.Client.Core.Features.Sounds.FavoriteSounds;
using Frever.Client.Core.Features.Sounds.UserSounds.DataAccess;
using Frever.Client.Shared.Files;
using Frever.Client.Shared.Social.Services;
using Frever.ClientService.Contract.Social;
using Frever.ClientService.Contract.Sounds;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.Sounds.UserSounds;

internal sealed class UserSoundAssetService : IUserSoundAssetService
{
    private readonly string _userSoundCacheKey;

    private readonly ICache _cache;
    private readonly UserInfo _currentUser;
    private readonly IFavoriteSoundRepository _favoriteSoundRepo;
    private readonly IMapper _mapper;
    private readonly IParentalConsentValidationService _parentalConsent;
    private readonly ISocialSharedService _socialSharedService;
    private readonly IUserPermissionService _userPermissionService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMediaFingerprintingService _mediaFingerprintingService;

    private readonly IBlobCache<UserSoundFullInfo[]> _userSoundListCache;
    private readonly IUserSoundAssetRepository _userSoundRepo;
    private readonly IValidator<UserSoundCreateModel> _validator;

    public UserSoundAssetService(
        ICache cache,
        IMapper mapper,
        UserInfo currentUser,
        IUserSoundAssetRepository userSoundRepo,
        IBlobCache<UserSoundFullInfo[]> userSoundListCache,
        IUserPermissionService userPermissionService,
        IValidator<UserSoundCreateModel> validator,
        IParentalConsentValidationService parentalConsent,
        IFavoriteSoundRepository favoriteSoundRepo,
        ISocialSharedService socialSharedService,
        IFileStorageService fileStorageService,
        IMediaFingerprintingService mediaFingerprintingService
    )
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userSoundRepo = userSoundRepo ?? throw new ArgumentNullException(nameof(userSoundRepo));
        _userSoundListCache = userSoundListCache ?? throw new ArgumentNullException(nameof(userSoundListCache));
        _userPermissionService = userPermissionService ?? throw new ArgumentNullException(nameof(userPermissionService));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _parentalConsent = parentalConsent ?? throw new ArgumentNullException(nameof(parentalConsent));
        _favoriteSoundRepo = favoriteSoundRepo ?? throw new ArgumentNullException(nameof(favoriteSoundRepo));
        _socialSharedService = socialSharedService ?? throw new ArgumentNullException(nameof(socialSharedService));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _mediaFingerprintingService = mediaFingerprintingService ?? throw new ArgumentNullException(nameof(mediaFingerprintingService));

        _userSoundCacheKey = $"{nameof(UserSoundFullInfo)}".FreverAssetCacheKey().CachePerUser(_currentUser.UserMainGroupId);
    }

    public async Task<UserSoundFullInfo[]> GetUserSoundListAsync(UserSoundFilterModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        await _userPermissionService.EnsureCurrentUserActive();

        var userSounds = await GetUserSounds();

        var result = userSounds.Skip(model.Skip).Take(model.Take).ToArray();

        var ids = await _favoriteSoundRepo.GetFavoriteUserSoundIds(_currentUser, result.Select(e => e.Id));

        foreach (var item in result)
        {
            await _fileStorageService.InitUrls<UserSound>(item);
            item.IsFavorite = ids.Contains(item.Id);
        }

        return result;
    }

    public async Task<UserSoundFullInfo> GetUserSoundById(long id)
    {
        await _userPermissionService.EnsureCurrentUserActive();

        var result = await _userSoundRepo.GetUserSoundByIds(_currentUser, id)
                                         .Select(
                                              e => new UserSoundFullInfo
                                              {
                                                  Id = e.Id,
                                                  Name = e.Name,
                                                  Duration = e.Duration,
                                                  CreatedTime = e.CreatedTime,
                                                  UsageCount = e.UsageCount,
                                                  Files = e.Files,
                                                  Owner = new GroupShortInfo { Id = e.GroupId },
                                                  IsFavorite = _favoriteSoundRepo.GetSoundsByGroupId(_currentUser)
                                                                                      .Any(s => s.UserSoundId == id)
                                              }
                                          )
                                         .FirstOrDefaultAsync();

        var groupInfo = await _socialSharedService.GetGroupShortInfo(result.Owner.Id);
        result.Owner = groupInfo.GetValueOrDefault(result.Owner.Id);

        await _fileStorageService.InitUrls<UserSound>(result);

        return result;
    }

    public async Task<UserSoundFullInfo[]> GetUserSoundByIds(long[] ids)
    {
        if (ids == null || ids.Length == 0)
            return [];

        await _userPermissionService.EnsureCurrentUserActive();

        var userSounds = await _userSoundRepo.GetUserSoundByIds(_currentUser, ids)
                                             .Select(
                                                  e => new UserSoundFullInfo
                                                  {
                                                      Id = e.Id,
                                                      Name = e.Name,
                                                      Duration = e.Duration,
                                                      CreatedTime = e.CreatedTime,
                                                      UsageCount = e.UsageCount,
                                                      Files = e.Files,
                                                      Owner = new GroupShortInfo { Id = e.GroupId },
                                                      IsFavorite = _favoriteSoundRepo
                                                                       .GetSoundsByGroupId(_currentUser)
                                                                       .Any(s => s.UserSoundId == e.Id)
                                                  }
                                              )
                                             .ToArrayAsync();

        var groupInfo = await _socialSharedService.GetGroupShortInfo(userSounds.Select(e => e.Owner.Id).ToArray());
        foreach (var item in userSounds)
        {
            item.Owner = groupInfo.GetValueOrDefault(item.Owner.Id);
            await _fileStorageService.InitUrls<UserSound>(item);
        }

        return userSounds;
    }

    public async Task<UserSoundFullInfo> SaveUserSound(UserSoundCreateModel input)
    {
        await _userPermissionService.EnsureCurrentUserActive();
        await _parentalConsent.EnsureAudioUploadAllowed();

        await _validator.ValidateAndThrowAsync(input);

        var userSound = _mapper.Map<UserSound>(input);
        userSound.GroupId = _currentUser.UserMainGroupId;

        var uploader = _fileStorageService.CreateFileUploader();

        await _userSoundRepo.CreateUserSound(userSound);

        await uploader.UploadFiles<UserSound>(userSound);

        await _userSoundRepo.SaveChanges();
        await uploader.WaitForCompletion();

        await _cache.DeleteKeysWithInfix(_userSoundCacheKey.GetKeyWithoutVersion());

        var result = await GetUserSoundById(userSound.Id);

        if (result == null)
            throw AppErrorWithStatusCodeException.NotFound("User sound is not found or not accessible", "UserSoundNotFound");

        return result;
    }

    public async Task<UserSoundFullInfo> RenameUserSound(long id, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(newName));

        await _userPermissionService.EnsureCurrentUserActive();

        newName = newName.Trim();

        await _userSoundRepo.RenameUserSound(id, newName, _currentUser);

        await _cache.DeleteKeysWithInfix(_userSoundCacheKey.GetKeyWithoutVersion());

        var userSounds = await GetUserSounds();

        var result = userSounds.FirstOrDefault(e => e.Id == id);
        if (result == null)
            throw AppErrorWithStatusCodeException.NotFound("User sound is not found or not accessible", "UserSoundNotFound");

        return result;
    }

    public async Task<bool> ContainsCopyrightedContent(long id)
    {
        var userSound = await _userSoundRepo.GetUserSounds(_currentUser).FirstOrDefaultAsync(e => e.Id == id);
        if (userSound == null)
            throw AppErrorWithStatusCodeException.NotFound("User sound is not found or not accessible", "UserSoundNotFound");

        if (userSound.ContainsCopyrightedContent != null)
            return userSound.ContainsCopyrightedContent.Value;

        var mainFile = userSound.Files.Main();
        if (mainFile == null)
            throw AppErrorWithStatusCodeException.NotFound("User sound is not found or not accessible", "UserSoundNotFound");

        var moderationResult = await _mediaFingerprintingService.CheckS3File(mainFile.Path, TimeSpan.FromSeconds(userSound.Duration));
        userSound.ContainsCopyrightedContent = moderationResult.ContainsCopyrightedContent;
        userSound.CopyrightCheckResults = moderationResult.Response;
        await _userSoundRepo.SaveChanges();

        return moderationResult.ContainsCopyrightedContent;
    }

    private Task<UserSoundFullInfo[]> GetUserSounds()
    {
        return _userSoundListCache.GetOrCache(_userSoundCacheKey, GetData, TimeSpan.FromDays(1));

        Task<UserSoundFullInfo[]> GetData()
        {
            return _userSoundRepo.GetUserSounds(_currentUser)
                                 .AsNoTracking()
                                 .OrderByDescending(e => e.CreatedTime)
                                 .ProjectTo<UserSoundFullInfo>(_mapper.ConfigurationProvider)
                                 .OrderByDescending(e => e.CreatedTime)
                                 .ToArrayAsync();
        }
    }
}

public class UserSoundFilesConfig : DefaultFileMetadataConfiguration<UserSound>
{
    public UserSoundFilesConfig()
    {
        AddMainFile("mp3", false);
    }
}