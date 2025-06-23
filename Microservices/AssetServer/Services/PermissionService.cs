using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AssetServer.Services.Storage;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Models;
using Common.Models.Database.Interfaces;
using Common.Models.Files;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AssetServer.Services;

internal sealed class PermissionService : IPermissionService
{
    private static readonly MethodInfo MethodInfoGetAssetGroupQuery = typeof(PermissionService).GetMethod(nameof(GetAssetGroupQuery));

    // TODO: check, probably we can replace it with some interface
    private static readonly Type[] PublicEntities =
    [
        typeof(Album),
        typeof(InAppProductDetails),
        typeof(AiMakeUp),
        typeof(PromotedSong),
        typeof(Song)
    ];

    private readonly ILogger _log;
    private readonly IServiceProvider _serviceProvider;
    private readonly UserInfo _userContext;
    private readonly IUserPermissionService _permissionService;

    public PermissionService(
        IServiceProvider serviceProvider,
        UserInfo userContext,
        IUserPermissionService permissionService,
        ILoggerFactory loggerFactory
    )
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _userContext = userContext;
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));

        _log = loggerFactory.CreateLogger("AssetPermissionService");
    }

    public async Task<bool> HasPermissions(Type assetType, long id, FileTagInfo tags)
    {
        if (_userContext != null && await _permissionService.IsCurrentUserEmployee())
            return true;

        var isProtectedFile = typeof(IGroupAccessible).IsAssignableFrom(assetType);
        if (!isProtectedFile)
            return true;

        if (PublicEntities.Contains(assetType))
            return true;

        if (tags.Groups.Contains(Constants.PublicAccessGroupId))
            return true;

        if (_userContext != null && tags.Groups.Contains(_userContext))
            return true;

        if (assetType == typeof(UserSound))
            return await HasAccessToUserSound(id);

        if (tags.LevelId == null)
            _log.LogWarning(
                "Asset for {AssetTypeName} ID={Id} is level-accessible but miss LevelId tag. Fallback to getting level ID from database",
                assetType.Name,
                id
            );

        return await IsPermissionToAssetGrantedAsync(assetType, id, _userContext.UserId);
    }

    public bool IsFileProtected(Type assetType, FileType fileType)
    {
        return fileType != FileType.Thumbnail;
    }

    private async Task<bool> IsPermissionToAssetGrantedAsync(Type assetType, long assetId, long userId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IWriteDb>();

        // method is not async - we just return task;
        var entityType = db.Model.GetEntityTypes()
                           .FirstOrDefault(t => t.ClrType != null && (t.ClrType == assetType || t.ClrType.IsAssignableFrom(assetType)));

        if (entityType == null)
            throw new Exception($"Asset type {assetType} is not an entity model and not supported.");

        var isValidType = typeof(IEntity).IsAssignableFrom(entityType.ClrType) &&
                          typeof(IGroupAccessible).IsAssignableFrom(entityType.ClrType);

        if (!isValidType)
            throw new Exception(
                $"Permission denied: Asset:{assetType} model is not properly defined and implements no required interfaces."
            );

        var assetGroup = (IQueryable<long>) MethodInfoGetAssetGroupQuery.MakeGenericMethod(entityType.ClrType)
                                                                        .Invoke(this, [db, assetId]);

        var userGroups = db.UserAndGroup.Where(x => x.UserId == userId);

        var permissionGrantedQuery = from userGroup in userGroups
                                     join assetGroupId in assetGroup on userGroup.GroupId equals assetGroupId
                                     select 1;

        return await permissionGrantedQuery.AnyAsync();
    }

    private async Task<bool> HasAccessToUserSound(long userSoundId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IWriteDb>();

        return await db.UserSound.AnyAsync(e => e.Id == userSoundId && e.GroupId == _userContext.UserMainGroupId);
    }

    public IQueryable<long> GetAssetGroupQuery<TEntity>(IWriteDb db, long assetId)
        where TEntity : class, IEntity, IGroupAccessible
    {
        var query = db.Set<TEntity>().Where(x => x.Id == assetId).Select(x => x.GroupId);

        return query;
    }
}