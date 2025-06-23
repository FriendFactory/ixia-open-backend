using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Frever.Cache;
using Frever.ClientService.Contract.Locales;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.Localizations;

public interface ILocalizationService
{
    Task<CountryDto[]> GetCountryList();

    Task<CountryDto> GetCountryByIso3Code(string iso3Code);

    Task<LanguageDto[]> GetCrewLanguages();

    Task<string> GetStartUpLocalization(string isoCode);

    Task<(bool IsModified, LocalizationResponse Response)> GetLocalization(string isoCode, string version);
}

internal sealed class LocalizationService(
    IMapper mapper,
    IBlobCache<CountryDto[]> countriesCache,
    IBlobCache<LanguageDto[]> languagesCache,
    IBlobCache<LocalizationInfo[]> localizationCache,
    IBlobCache<List<LocalizationInternal>> csvLocalizationCache,
    ILocalizationRepository repo
) : ILocalizationService
{
    private static readonly CsvConfiguration Configuration =
        new(CultureInfo.CurrentCulture) {HasHeaderRecord = true, Delimiter = ",", Encoding = Encoding.UTF8};

    private readonly IBlobCache<CountryDto[]> _countriesCache = countriesCache ?? throw new ArgumentNullException(nameof(countriesCache));

    private readonly IBlobCache<List<LocalizationInternal>> _csvLocalizationCache =
        csvLocalizationCache ?? throw new ArgumentNullException(nameof(csvLocalizationCache));

    private readonly IBlobCache<LanguageDto[]> _languagesCache = languagesCache ?? throw new ArgumentNullException(nameof(languagesCache));

    private readonly IBlobCache<LocalizationInfo[]> _localizationCache =
        localizationCache ?? throw new ArgumentNullException(nameof(localizationCache));

    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly ILocalizationRepository _repo = repo ?? throw new ArgumentNullException(nameof(repo));

    public Task<CountryDto[]> GetCountryList()
    {
        return _countriesCache.GetOrCache($"{nameof(CountryDto)}".FreverCacheKey(), GetData, TimeSpan.FromDays(5));

        Task<CountryDto[]> GetData()
        {
            return _repo.GetCountries().ProjectTo<CountryDto>(_mapper.ConfigurationProvider).ToArrayAsync();
        }
    }

    public async Task<CountryDto> GetCountryByIso3Code(string iso3Code)
    {
        if (string.IsNullOrWhiteSpace(iso3Code))
            throw new ArgumentNullException(nameof(iso3Code));

        var countries = await GetCountryList();

        return countries.FirstOrDefault(c => StringComparer.OrdinalIgnoreCase.Equals(c.Iso3Code, iso3Code));
    }

    public Task<LanguageDto[]> GetCrewLanguages()
    {
        return _languagesCache.GetOrCache($"{nameof(LanguageDto)}".FreverCacheKey(), GetData, TimeSpan.FromDays(5));

        Task<LanguageDto[]> GetData()
        {
            return _repo.GetLanguages().Where(e => e.AvailableForCrew).ProjectTo<LanguageDto>(_mapper.ConfigurationProvider).ToArrayAsync();
        }
    }

    public async Task<string> GetStartUpLocalization(string isoCode)
    {
        var cachedLocalization = await GetCachedLocalization();

        var filtered = cachedLocalization.Where(e => e.IsStartupItem).ToArray();

        if (!filtered.Any(e => e.Values.ContainsKey(isoCode)))
            isoCode = Constants.FallbackLocalizationCode;

        var languages = await _repo.GetLanguages()
                                   .Where(e => new[] {isoCode, Constants.FallbackLocalizationCode}.Contains(e.IsoCode))
                                   .ToArrayAsync();

        var result = await ToCsv(filtered, languages);

        return result;
    }

    public async Task<(bool IsModified, LocalizationResponse Response)> GetLocalization(string isoCode, string version)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
            throw new ArgumentNullException(isoCode);

        var cachedLocalization = await GetCachedLocalization();

        if (!cachedLocalization.Any(e => e.Values.ContainsKey(isoCode)))
            isoCode = Constants.FallbackLocalizationCode;

        var key = nameof(LocalizationInternal).FreverCacheKey();

        var localizations = await _csvLocalizationCache.TryGet(key);
        if (localizations != null)
        {
            if (version != null && localizations.Any(e => e.Version.Equals(version, StringComparison.OrdinalIgnoreCase)))
                return (false, null);

            var existing = localizations.FirstOrDefault(e => e.LanguageIsoCode.Equals(isoCode, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                return (true, new LocalizationResponse {Version = existing.Version, Value = existing.Value});
        }

        var languages = await _repo.GetLanguages()
                                   .Where(e => new[] {isoCode, Constants.FallbackLocalizationCode}.Contains(e.IsoCode))
                                   .ToArrayAsync();

        var result = await ToCsv(cachedLocalization, languages);
        var newVersion = GetVersion(result);
        var newLocalization = new LocalizationInternal {LanguageIsoCode = isoCode, Version = newVersion, Value = result};

        if (localizations is null)
            await _csvLocalizationCache.GetOrCache(
                key,
                () => Task.FromResult(new List<LocalizationInternal> {newLocalization}),
                TimeSpan.FromDays(7)
            );
        else
            await _csvLocalizationCache.TryModifyInPlace(
                key,
                e =>
                {
                    e.Add(newLocalization);
                    return Task.FromResult(e);
                }
            );

        return newVersion == version ? (false, null) : (true, new LocalizationResponse {Version = newVersion, Value = result});
    }

    private Task<LocalizationInfo[]> GetCachedLocalization()
    {
        return _localizationCache.GetOrCache(nameof(LocalizationInfo).FreverCacheKey(), ReadFromDb, TimeSpan.FromDays(7));

        Task<LocalizationInfo[]> ReadFromDb()
        {
            return _repo.GetLocalizations()
                        .Select(
                             e => new LocalizationInfo
                                  {
                                      Key = e.Key,
                                      Type = e.Type,
                                      Description = e.Description,
                                      IsStartupItem = e.IsStartupItem,
                                      Values = JsonConvert
                                         .DeserializeObject<Dictionary<string, string>>(e.Value)
                                  }
                         )
                        .ToArrayAsync();
        }
    }

    private static async Task<string> ToCsv(IEnumerable<LocalizationInfo> localization, Language[] languages)
    {
        using var memoryStream = new MemoryStream();
        await using var streamWriter = new StreamWriter(memoryStream);
        await using var csvWriter = new CsvWriter(streamWriter, Configuration);

        foreach (var heading in Constants.StaticHeaders.Concat(languages.Select(e => e.Name)))
            csvWriter.WriteField(heading);

        await csvWriter.NextRecordAsync();

        foreach (var item in localization)
        {
            csvWriter.WriteField(item.Key);
            csvWriter.WriteField(item.Type);
            csvWriter.WriteField(item.Description);

            foreach (var lang in languages)
                csvWriter.WriteField(item.Values.GetValueOrDefault(lang.IsoCode));

            await csvWriter.NextRecordAsync();
        }

        await streamWriter.FlushAsync();

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    private static string GetVersion(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));

        var result = bytes.Select(e => e.ToString("x2"));

        return string.Concat(result);
    }
}

public class LocalizationInternal
{
    public string LanguageIsoCode { get; init; }
    public string Version { get; init; }
    public string Value { get; init; }
}

public class LocalizationResponse
{
    public string Version { get; init; }
    public string Value { get; init; }
}