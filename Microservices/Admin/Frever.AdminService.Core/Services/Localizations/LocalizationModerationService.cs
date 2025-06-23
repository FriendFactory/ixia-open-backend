using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using Common.Infrastructure;
using Common.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Frever.AdminService.Core.Utils;
using Frever.Cache.Resetting;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Frever.AdminService.Core.Services.Localizations;

internal sealed class LocalizationModerationService(ICacheReset cacheReset, IWriteDb db, IUserPermissionService permissionService)
    : ILocalizationModerationService
{
    private const int ImportBatchLimit = 100;

    private static readonly CsvConfiguration Configuration =
        new(CultureInfo.CurrentCulture) {HasHeaderRecord = true, Delimiter = ",", Encoding = Encoding.UTF8};

    private readonly ICacheReset _cacheReset = cacheReset ?? throw new ArgumentNullException(nameof(cacheReset));
    private readonly IWriteDb _db = db ?? throw new ArgumentNullException(nameof(db));

    private readonly IUserPermissionService _permissionService =
        permissionService ?? throw new ArgumentNullException(nameof(permissionService));

    public async Task<ResultWithCount<LocalizationDto>> GetLocalization(
        ODataQueryOptions<LocalizationDto> options,
        string isoCode,
        string value
    )
    {
        await _permissionService.EnsureHasCategoryReadAccess();

        string filer = null;

        if (!string.IsNullOrWhiteSpace(isoCode))
            filer += $"\"{isoCode.ToLower()}\"";

        if (!string.IsNullOrWhiteSpace(value))
            filer += $":\"{value.ToLower()}";

        return await _db.Localization.Where(e => filer == null || e.Value.ToLower().Contains(filer))
                        .OrderBy(e => e.Key)
                        .Select(
                             e => new LocalizationDto
                                  {
                                      Key = e.Key,
                                      Type = e.Type,
                                      Description = e.Description,
                                      Values = JsonConvert.DeserializeObject<Dictionary<string, string>>(e.Value)
                                  }
                         )
                        .ExecuteODataRequestWithCount(options);
    }

    public async Task SaveLocalization(LocalizationDto model)
    {
        await _permissionService.EnsureHasCategoryFullAccess();

        ArgumentException.ThrowIfNullOrEmpty(model.Key);

        if (model.Values is null || !model.Values.ContainsKey(Constants.FallbackLocalizationCode))
            throw AppErrorWithStatusCodeException.BadRequest(
                $"Localization must contain {Constants.FallbackLocalizationCode} value",
                "FallbackLocalizationValue"
            );

        var dbCodes = await _db.Language.Where(e => model.Values.Keys.Contains(e.IsoCode)).Select(e => e.IsoCode).ToArrayAsync();
        var missingCodes = model.Values.Keys.Except(dbCodes).ToArray();
        if (missingCodes.Length > 0)
            throw AppErrorWithStatusCodeException.BadRequest(
                $"Languages with ISO codes {string.Join(',', missingCodes)} not found",
                "ISOCodesNotFound"
            );

        var localization = _db.Localization.FirstOrDefault(e => e.Key == model.Key);
        if (localization is null)
        {
            localization = new Localization {Key = model.Key};
            await _db.Localization.AddAsync(localization);
        }

        localization.Type = model.Type?.Trim();
        localization.Description = model.Description?.Trim();
        localization.Value = JsonConvert.SerializeObject(model.Values);

        await _db.SaveChangesAsync();
        await _cacheReset.ResetOnDependencyChange(typeof(Localization), null);
    }

    public async Task DeleteLocalizationByKey(string key)
    {
        await _permissionService.EnsureHasCategoryFullAccess();

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentNullException(nameof(key));

        var localization = _db.Localization.FirstOrDefault(e => e.Key == key);
        if (localization != null)
        {
            _db.Localization.Remove(localization);

            await _db.SaveChangesAsync();
            await _cacheReset.ResetOnDependencyChange(typeof(Localization), null);
        }
    }

    public async Task<byte[]> ExportLocalizationToCsv(string[] keys)
    {
        await _permissionService.EnsureHasCategoryFullAccess();

        keys ??= [];

        var localization = await _db.Localization.Where(e => keys.Length == 0 || keys.Contains(e.Key))
                                    .Select(e => ToLocalizationDto(e))
                                    .ToArrayAsync();

        var isoCodes = localization.SelectMany(e => e.Values.Keys).Distinct();
        var languages = await _db.Language.Where(e => isoCodes.Contains(e.IsoCode)).Select(e => new {e.Name, e.IsoCode}).ToArrayAsync();

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

        return memoryStream.ToArray();
    }

    public async Task ImportLocalizationFromCsv(IFormFile file, ImportType type)
    {
        await _permissionService.EnsureHasCategoryFullAccess();

        ArgumentNullException.ThrowIfNull(file);

        var localization = await FromCsv(file, GetLanguages);
        if (localization.Count == 0)
            return;

        if (localization.Any(e => e.Value == null || !e.Value.Values.ContainsKey(Constants.FallbackLocalizationCode)))
            throw AppErrorWithStatusCodeException.BadRequest(
                $"Localization must contain {Constants.FallbackLocalizationCode} value",
                "FallbackLocalizationValue"
            );

        await using var transaction = await _db.BeginTransaction();

        var existingKeys = await _db.Localization.Where(e => localization.Keys.Contains(e.Key)).Select(e => e.Key).ToArrayAsync();
        var newLocalization = localization.Values.Where(e => !existingKeys.Contains(e.Key)).ToArray();
        var existingLocalization = localization.Values.Except(newLocalization);

        if (type == ImportType.Replace)
        {
            var keysCommaSeparated = string.Join(",", existingKeys.Select(a => $"'{a}'"));
            var deleteSql = $"delete from \"Localization\" where \"Key\" not in ({keysCommaSeparated})";
            await _db.ExecuteSqlRawAsync(deleteSql);
        }

        if (type is ImportType.Replace or ImportType.Merge)
        {
            var groups = existingLocalization.Select((templateId, index) => new {Item = templateId, Group = index / ImportBatchLimit})
                                             .GroupBy(a => a.Group, a => a.Item)
                                             .Select(e => e.ToArray())
                                             .ToArray();

            foreach (var group in groups)
                await UpdateLocalization(group, type);
        }

        if (newLocalization.Length > 0)
            await _db.Localization.AddRangeAsync(newLocalization.Select(ToLocalization));

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await _cacheReset.ResetOnDependencyChange(typeof(Localization), null);
    }

    private async Task UpdateLocalization(IReadOnlyCollection<LocalizationDto> group, ImportType type)
    {
        if (group.Count == 0)
            return;

        var keys = group.Select(k => k.Key);
        var dbLocalization = await _db.Localization.Where(e => keys.Contains(e.Key)).ToDictionaryAsync(e => e.Key, e => e);

        foreach (var item in group)
        {
            var localization = dbLocalization.GetValueOrDefault(item.Key);
            if (localization is null)
                continue;

            localization.Type = item.Type;
            localization.Description = item.Description;

            if (type == ImportType.Replace)
            {
                localization.Value = JsonConvert.SerializeObject(item.Values);
            }
            else
            {
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(localization.Value);

                foreach (var val in item.Values.Where(v => values.ContainsKey(v.Key)))
                    values[val.Key] = val.Value;

                localization.Value = JsonConvert.SerializeObject(values);
            }
        }
    }

    private static async Task<Dictionary<string, LocalizationDto>> FromCsv(
        IFormFile file,
        Func<string[], Task<Dictionary<string, string>>> getLanguages
    )
    {
        using var streamReader = new StreamReader(file.OpenReadStream());
        using var csvReader = new CsvReader(streamReader, Configuration);

        await csvReader.ReadAsync();
        csvReader.ReadHeader();

        var dynamicHeaders = csvReader.HeaderRecord?.Except(Constants.StaticHeaders).ToArray() ?? [];
        var languages = await getLanguages(dynamicHeaders);

        var result = new Dictionary<string, LocalizationDto>();

        while (await csvReader.ReadAsync())
        {
            var row = new LocalizationDto
                      {
                          Key = csvReader.GetField<string>(Constants.StaticHeaders[0])?.Trim(),
                          Type = csvReader.GetField<string>(Constants.StaticHeaders[1])?.Trim(),
                          Description = csvReader.GetField<string>(Constants.StaticHeaders[2])?.Trim(),
                          Values = new Dictionary<string, string>()
                      };

            if (string.IsNullOrWhiteSpace(row.Key))
                throw new ArgumentNullException(nameof(row.Key));

            foreach (var header in dynamicHeaders)
            {
                var isoCode = languages[header];
                var value = csvReader.GetField<string>(header)?.Trim();

                if (!string.IsNullOrWhiteSpace(value))
                    row.Values[isoCode] = value;
            }

            result[row.Key] = row;
        }

        return result;
    }

    private async Task<Dictionary<string, string>> GetLanguages(string[] languages)
    {
        var dbLanguages = await _db.Language.Where(e => languages.Contains(e.Name)).Select(e => new {e.Name, e.IsoCode}).ToArrayAsync();

        var result = new Dictionary<string, string>();
        foreach (var l in dbLanguages)
            result[l.Name] = l.IsoCode;

        var missing = languages.Where(l => !result.ContainsKey(l)).ToArray();
        if (missing.Length > 0)
            throw AppErrorWithStatusCodeException.BadRequest($"Languages {string.Join(',', missing)} not found", "LanguagesNotFound");

        return result;
    }

    private static LocalizationDto ToLocalizationDto(Localization e)
    {
        return new LocalizationDto
               {
                   Key = e.Key,
                   Type = e.Type,
                   Description = e.Description,
                   Values = JsonConvert.DeserializeObject<Dictionary<string, string>>(e.Value)
               };
    }

    private static Localization ToLocalization(LocalizationDto e)
    {
        return new Localization
               {
                   Key = e.Key,
                   Type = e.Type,
                   Description = e.Description,
                   Value = JsonConvert.SerializeObject(e.Values)
               };
    }
}