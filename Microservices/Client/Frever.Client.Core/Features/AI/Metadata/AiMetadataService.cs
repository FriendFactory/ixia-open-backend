using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Infrastructure;
using Frever.Cache;
using Frever.Client.Core.Features.Sounds.Song;
using Frever.Client.Shared.ActivityRecording;
using Frever.Client.Shared.AI.ComfyUi;
using Frever.Client.Shared.AI.Metadata;
using Frever.Client.Shared.Files;
using Frever.ClientService.Contract.Metadata;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Features.PersonalFeed;
using Frever.Videos.Shared.MusicGeoFiltering;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.AI.Metadata;

public class AiMetadataService(
    UserInfo currentUser,
    IMapper mapper,
    IAiMetadataRepository repo,
    IUserPermissionService userPermissionService,
    IFileStorageService fileStorage,
    ISongAssetService songAssetService,
    IAiWorkflowMetadataService workflowMetadataService,
    IUserActivityRecordingService activityRecordingService,
    ICurrentLocationProvider locationProvider,
    IPersonalFeedRefreshingService personalFeedRefreshingService,
    IBlobCache<AiArtStyle[]> artStyleCache,
    IBlobCache<AiLlmPrompt[]> promptCache,
    IBlobCache<AiSpeakerMode[]> speakerCache,
    IBlobCache<AiLanguageMode[]> languageCache,
    IBlobCache<GenderDto[]> genderCache,
    IBlobCache<AiMakeUp[]> makeUpCache
) : IAiMetadataService
{
    private static readonly WardrobeModeDto[] WardrobeModes = GetWardrobeModes();
    private static readonly CharacterCreationOptions Options = GetCharacterCreationOptions();

    public async Task<ArtStyleDto[]> GetArtStyles(long? genderId, int skip, int take)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var all = await GetCachedArtStyles();

        var filtered = all.Where(e => !genderId.HasValue || e.GenderId == genderId)
                          .OrderBy(e => e.SortOrder)
                          .ThenBy(e => e.Id)
                          .Skip(skip)
                          .Take(take);

        var result = mapper.Map<ArtStyleDto[]>(filtered);

        await fileStorage.InitUrls<AiArtStyle>(result);

        return result;
    }

    public async Task<MakeUpDto[]> GetMakeUps(long? categoryId, int skip, int take)
    {
        await userPermissionService.EnsureCurrentUserActive();

        var all = await GetCachedMakeUps();

        var filtered = all.Where(e => !categoryId.HasValue || e.CategoryId == categoryId)
                          .OrderBy(e => e.SortOrder)
                          .ThenBy(e => e.Id)
                          .Skip(skip)
                          .Take(take);

        var result = mapper.Map<MakeUpDto[]>(filtered);

        await fileStorage.InitUrls<AiMakeUp>(result);

        return result;
    }

    public async Task<PromptDataDto> GetLlmPromptsData(PromptInput input)
    {
        await userPermissionService.EnsureCurrentUserActive();

        ArgumentNullException.ThrowIfNull(input);

        if (input.Keys == null || input.Keys.Length == 0)
            return new PromptDataDto();

        var all = await GetCachedPrompts();

        var result = new Dictionary<string, string>();
        foreach (var key in input.Keys)
        {
            var prompt = all.FirstOrDefault(x => x.Key == key);
            if (prompt == null)
                throw AppErrorWithStatusCodeException.NotFound("Prompt not found for key: " + key, "Prompt not found");

            result[key] = prompt.Prompt;
        }

        return new PromptDataDto {Prompts = result};
    }

    public async Task<SpeakerModeDto[]> GetSpeakerModes(int skip, int take)
    {
        var all = await GetCachedSpeakerModes();

        var result = mapper.Map<SpeakerModeDto[]>(all.OrderBy(e => e.SortOrder).ThenBy(e => e.Id).Skip(skip).Take(take));
        await fileStorage.InitUrls<AiSpeakerMode>(result);
        return result;
    }

    public async Task<LanguageModeDto[]> GetLanguageModes(int skip, int take)
    {
        var all = await GetCachedLanguageModes();

        var result = mapper.Map<LanguageModeDto[]>(all.OrderBy(e => e.SortOrder).ThenBy(e => e.Id).Skip(skip).Take(take));
        await fileStorage.InitUrls<AiLanguageMode>(result);
        return result;
    }

    public async Task<MetadataDto> GetMetadata()
    {
        await userPermissionService.EnsureCurrentUserActive();

        var location = await locationProvider.Get();

        var metadata = new MetadataDto
                       {
                           AllowedCharactersCount = 20,
                           CharacterCreationOptions = Options,
                           WardrobeModes = WardrobeModes,
                           Genders = await GetCachedGenders(),
                           Genres = await GetCachedGenres(location.CountryIso3Code),
                           MakeUpCategories = await GetCachedMakeUpCategories(),
                           Workflows = await workflowMetadataService.Get()
                       };

        await Task.WhenAll(RecordUserLogin(), RefreshPersonalFeed(location));

        return metadata;
    }

    public async Task<Dictionary<long, AiArtStyle>> GetArtStyleByIdsInternal(IEnumerable<long> ids)
    {
        var all = await GetCachedArtStyles();
        return all.Where(e => ids.Contains(e.Id)).ToDictionary(e => e.Id, e => e);
    }

    public async Task<AiArtStyle> GetArtStyleByIdInternal(long id)
    {
        var all = await GetCachedArtStyles();
        return all.FirstOrDefault(e => e.Id == id);
    }

    public async Task<AiMakeUp> GetMakeUpByIdInternal(long id)
    {
        var all = await GetCachedMakeUps();

        return all.FirstOrDefault(e => e.Id == id);
    }

    public async Task<AiSpeakerMode> GetSpeakerModeOrDefaultInternal(long? speakerModeId)
    {
        var all = await GetCachedSpeakerModes();
        if (!speakerModeId.HasValue)
            return all.FirstOrDefault(e => e.IsDefault) ?? all.FirstOrDefault();

        var result = all.FirstOrDefault(e => e.Id == speakerModeId);
        if (result == null)
            throw AppErrorWithStatusCodeException.NotFound("Speaker mode not found", "Speaker mode not found");

        return result;
    }

    public async Task<AiLanguageMode> GetLanguageModeOrDefaultInternal(long? languageModeId)
    {
        var all = await GetCachedLanguageModes();
        if (!languageModeId.HasValue)
            return all.FirstOrDefault(e => e.IsDefault) ?? all.FirstOrDefault();

        var result = all.FirstOrDefault(e => e.Id == languageModeId);
        if (result == null)
            throw AppErrorWithStatusCodeException.NotFound("Language mode not found", "Language mode not found");

        return result;
    }

    public async Task<string> GetPopulatedPromptInternal(string key, Dictionary<string, string> parameters)
    {
        var all = await GetCachedPrompts();

        var template = all.FirstOrDefault(e => e.Key == key)?.Prompt;
        if (template == null)
            throw AppErrorWithStatusCodeException.NotFound("Prompt not found", "Prompt not found");

        return parameters.Aggregate(template, (current, placeholder) => current.Replace($"[{placeholder.Key}]", placeholder.Value));
    }

    private async Task<GenreDto[]> GetCachedGenres(string countryIso3Code)
    {
        var genres = await songAssetService.GetAvailableGenres(countryIso3Code);
        return mapper.Map<GenreDto[]>(genres);
    }

    private Task<AiArtStyle[]> GetCachedArtStyles()
    {
        return artStyleCache.GetOrCache($"{nameof(AiArtStyle)}".FreverAssetCacheKey(), repo.GetAiArtStyles, TimeSpan.FromDays(1));
    }

    private Task<AiLlmPrompt[]> GetCachedPrompts()
    {
        return promptCache.GetOrCache($"{nameof(AiLlmPrompt)}".FreverAssetCacheKey(), repo.GetAiLlmPrompts, TimeSpan.FromDays(1));
    }

    private Task<GenderDto[]> GetCachedGenders()
    {
        return genderCache.GetOrCache(
            nameof(GenderDto).FreverCacheKey(),
            () => repo.GetGenders().ProjectTo<GenderDto>(mapper.ConfigurationProvider).ToArrayAsync(),
            TimeSpan.FromDays(1)
        );
    }

    private Task<AiMakeUp[]> GetCachedMakeUps()
    {
        return makeUpCache.GetOrCache(nameof(MakeUpDto).FreverCacheKey(), repo.GetMakeUps, TimeSpan.FromDays(1));
    }

    private Task<AiSpeakerMode[]> GetCachedSpeakerModes()
    {
        return speakerCache.GetOrCache($"{nameof(AiSpeakerMode)}".FreverAssetCacheKey(), repo.GetAiSpeakerModes, TimeSpan.FromDays(7));
    }

    private Task<AiLanguageMode[]> GetCachedLanguageModes()
    {
        return languageCache.GetOrCache($"{nameof(AiLanguageMode)}".FreverAssetCacheKey(), repo.GetAiLanguageModes, TimeSpan.FromDays(7));
    }

    private async Task<MakeUpCategoryDto[]> GetCachedMakeUpCategories()
    {
        await userPermissionService.EnsureCurrentUserActive();

        var all = await GetCachedMakeUps();

        return mapper.Map<MakeUpCategoryDto[]>(all.Select(e => e.Category).DistinctBy(e => e.Id));
    }

    private static WardrobeModeDto[] GetWardrobeModes()
    {
        var wardrobeModes = (WardrobeMode[]) Enum.GetValues(typeof(WardrobeMode));
        return wardrobeModes.Select(e => new WardrobeModeDto {Id = (long) e, Name = e.ToString()}).ToArray();
    }

    private static CharacterCreationOptions GetCharacterCreationOptions()
    {
        return new CharacterCreationOptions
               {
                   Ethnicities =
                   [
                       "African", "Asian", "European", "North American", "South American",
                       "Australian"
                   ],
                   HairColors = ["Black", "Brown", "Blonde", "Red", "White"],
                   HairStyles = ["Long", "Short", "Curly", "Straight", "Wavy"]
               };
    }

    private Task RefreshPersonalFeed(LocationInfo location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return personalFeedRefreshingService.RefreshFeed(currentUser, location.Lon, location.Lat);
    }

    private Task RecordUserLogin()
    {
        return activityRecordingService.OnLogin();
    }
}