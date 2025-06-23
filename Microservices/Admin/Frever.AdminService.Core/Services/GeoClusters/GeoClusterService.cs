using System;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common.Infrastructure;
using FluentValidation;
using Frever.AdminService.Core.Utils;
using Frever.Cache.Resetting;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Frever.Videos.Shared.GeoClusters;
using Microsoft.AspNet.OData.Query;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Frever.AdminService.Core.Services.GeoClusters;

internal sealed class GeoClusterService(
    IMapper mapper,
    IWriteDb mainDb,
    ICacheReset cacheReset,
    IUserPermissionService permissionService,
    IValidator<GeoClusterDto> validator
) : IGeoClusterService
{
    private static readonly JsonSerializerSettings JsonSerializerSettings =
        new() {ContractResolver = new CamelCasePropertyNamesContractResolver()};

    private readonly ICacheReset _cacheReset = cacheReset ?? throw new ArgumentNullException(nameof(cacheReset));
    private readonly IWriteDb _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IValidator<GeoClusterDto> _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    private readonly IUserPermissionService _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));

    public async Task<ResultWithCount<GeoClusterDto>> GetGeoClusters(ODataQueryOptions<GeoClusterDto> options)
    {
        await _permissionService.EnsureHasCategoryReadAccess();

        return await _mainDb.GeoCluster.ProjectTo<GeoClusterDto>(_mapper.ConfigurationProvider).ExecuteODataRequestWithCount(options);
    }

    public async Task SaveGeoCluster(GeoClusterDto model)
    {
        await _permissionService.EnsureHasCategoryFullAccess();

        await _validator.ValidateAndThrowAsync(model);

        if (await _mainDb.GeoCluster.AnyAsync(e => e.Priority == model.Priority && e.Id != model.Id))
            throw AppErrorWithStatusCodeException.BadRequest("GeoCluster priority must be unique", "UniqueGeoClusterPriority");

        await ValidateCountries(model);

        var cluster = await _mainDb.GeoCluster.FindAsync(model.Id);
        if (cluster is null)
        {
            cluster = new GeoCluster();
            await _mainDb.GeoCluster.AddAsync(cluster);
        }

        _mapper.Map(model, cluster);

        await _mainDb.SaveChangesAsync();

        await _cacheReset.ResetOnDependencyChange(typeof(GeoCluster), null);
    }

    public async Task UpdateGeoCluster(long id, JObject model)
    {
        await _permissionService.EnsureHasCategoryFullAccess();

        ArgumentNullException.ThrowIfNull(model);

        var dbCluster = await _mainDb.GeoCluster.FindAsync(id);
        if (dbCluster == null)
            throw AppErrorWithStatusCodeException.NotFound($"GeoCluster ID={id} is not found or not accessible", "TaskNotFound");

        var cluster = new GeoClusterDto();

        var existingJson = JsonConvert.SerializeObject(dbCluster);
        JsonConvert.PopulateObject(existingJson, cluster, JsonSerializerSettings);
        JsonConvert.PopulateObject(model.ToString(), cluster, JsonSerializerSettings);

        cluster.Id = dbCluster.Id;

        await SaveGeoCluster(cluster);
    }

    public async Task DeleteGeoCluster(long id)
    {
        await _permissionService.EnsureHasCategoryFullAccess();

        var cluster = await _mainDb.GeoCluster.FindAsync(id);
        if (cluster == null)
            throw AppErrorWithStatusCodeException.NotFound($"GeoCluster ID={id} is not found or not accessible", "TaskNotFound");

        _mainDb.GeoCluster.Remove(cluster);

        await _mainDb.SaveChangesAsync();
    }

    private async Task ValidateCountries(GeoClusterDto model)
    {
        var countries = model.HideForUserFromCountry.Union(model.ShowToUserFromCountry)
                             .Union(model.ExcludeVideoFromCountry)
                             .Union(model.IncludeVideoFromCountry)
                             .Where(e => !GeoClusterProvider.IncludeAll.Contains(e));

        var dbCountries = await _mainDb.Country.Where(e => countries.Contains(e.ISOName)).Select(e => e.ISOName).Distinct().ToArrayAsync();

        var invalidCountriesData = countries.Where(c => !dbCountries.Contains(c)).ToArray();
        if (invalidCountriesData.Length > 0)
            throw AppErrorWithStatusCodeException.BadRequest(
                $"Can't save GeoCluster with countries {string.Join(",", invalidCountriesData)}",
                "InvalidCountriesData"
            );

        var languages = model.ExcludeVideoWithLanguage.Union(model.IncludeVideoWithLanguage)
                             .Union(model.HideForUserWithLanguage)
                             .Union(model.ShowForUserWithLanguage)
                             .Where(e => !GeoClusterProvider.IncludeAll.Contains(e));

        var dbLanguages = await _mainDb.Language.Where(e => languages.Contains(e.IsoCode)).Select(e => e.IsoCode).Distinct().ToArrayAsync();

        var invalidLanguagesData = languages.Where(c => !dbLanguages.Contains(c)).ToArray();
        if (invalidLanguagesData.Length > 0)
            throw AppErrorWithStatusCodeException.BadRequest(
                $"Can't save GeoCluster with languages {string.Join(",", invalidLanguagesData)}",
                "InvalidLanguagesData"
            );
    }
}