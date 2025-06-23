using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using Common.Infrastructure;
using Common.Infrastructure.Caching;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Core.Services.RoleModeration;

public class RoleModerationService(ICache cache, IWriteDb mainDb, IUserPermissionService permissionService) : IRoleModerationService
{
    private static readonly Dictionary<string, AccessScopeDto> AccessScopes = GetAllAccessScopes();

    private readonly ICache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly IWriteDb _mainDb = mainDb ?? throw new ArgumentNullException(nameof(mainDb));

    private readonly IUserPermissionService _permissionService =
        permissionService ?? throw new ArgumentNullException(nameof(permissionService));

    public async Task<AccessScopeDto[]> GetAccessScopes()
    {
        await _permissionService.EnsureHasSettingsAccess();

        return AccessScopes.Values.ToArray();
    }

    public async Task<AccessScopeDto[]> GetUserAccessScopes(long groupId)
    {
        var scopes = await _mainDb.UserRole.Where(e => e.GroupId == groupId)
                                  .Select(e => e.Role)
                                  .SelectMany(e => e.RoleAccessScope)
                                  .Select(e => e.AccessScope)
                                  .Distinct()
                                  .ToArrayAsync();

        var result = scopes.Select(e => AccessScopes.GetValueOrDefault(e)).ToArray();

        return result;
    }

    public async Task<RoleDto[]> GetRoles(int skip, int take)
    {
        await _permissionService.EnsureHasSettingsAccess();

        var roles = await _mainDb.Role.Include(e => e.RoleAccessScope).Skip(skip).Take(take).ToArrayAsync();

        Func<RoleAccessScope, AccessScopeDto> selector =
            s => new AccessScopeDto {Value = s.AccessScope, Name = AccessScopes.GetValueOrDefault(s.AccessScope).Name};

        var result = roles.Select(
                               r => new RoleDto
                                    {
                                        Id = r.Id,
                                        Name = r.Name,
                                        AccessScopes = r.RoleAccessScope.OrderBy(e => e.AccessScope).Select(selector).ToArray()
                                    }
                           )
                          .ToArray();

        return result;
    }

    public async Task<UserRoleDto[]> GetUserRoles(string email, long? roleId, int skip, int take)
    {
        await _permissionService.EnsureHasSettingsAccess();

        var query = _mainDb.UserRole.Where(e => roleId == null || e.RoleId == roleId);
        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(e => _mainDb.User.Any(u => u.MainGroupId == e.GroupId && u.Email.StartsWith(email)));

        var userRoles = await query.GroupBy(e => e.GroupId)
                                   .Skip(skip)
                                   .Take(take)
                                   .Select(e => new {e.Key, Roles = e.Select(r => new {r.Role.Id, r.Role.Name})})
                                   .ToArrayAsync();

        var groupIds = userRoles.Select(e => e.Key);
        var users = await _mainDb.User.Where(e => groupIds.Contains(e.MainGroupId))
                                 .Select(e => new {e.MainGroupId, e.Email, e.MainGroup.NickName})
                                 .ToDictionaryAsync(e => e.MainGroupId);

        var result = userRoles.Select(
                                   e => new UserRoleDto
                                        {
                                            GroupId = e.Key,
                                            Nickname = users.GetValueOrDefault(e.Key)?.NickName,
                                            Email = users.GetValueOrDefault(e.Key)?.Email,
                                            Roles = e.Roles.Select(r => new RoleDto {Id = r.Id, Name = r.Name}).OrderBy(r => r.Id).ToArray()
                                        }
                               )
                              .ToArray();

        return result;
    }

    public async Task SaveRole(RoleModel model)
    {
        await _permissionService.EnsureHasSettingsAccess();

        ArgumentNullException.ThrowIfNull(model);
        ArgumentException.ThrowIfNullOrEmpty(model.Name);

        if (model.AccessScopes.Any(e => !AccessScopes.ContainsKey(e)))
            throw AppErrorWithStatusCodeException.BadRequest("Invalid access scope", "InvalidAccessScope");

        var dbRole = await _mainDb.Role.Include(e => e.RoleAccessScope).FirstOrDefaultAsync(e => e.Id == model.Id);
        if (dbRole is null && model.Id != 0)
            throw AppErrorWithStatusCodeException.BadRequest("Role not found", "RoleNotFound");

        if (dbRole is null)
        {
            var lastId = await _mainDb.Role.OrderByDescending(e => e.Id).Select(e => e.Id).FirstOrDefaultAsync();
            dbRole = new Role
                     {
                         Id = lastId + 1,
                         Name = model.Name,
                         RoleAccessScope = [],
                         CreatedAt = DateTime.UtcNow
                     };
            await _mainDb.Role.AddAsync(dbRole);
        }

        var newRoles = model.AccessScopes.Where(e => dbRole.RoleAccessScope.All(s => s.AccessScope != e));
        dbRole.RoleAccessScope.AddRange(newRoles.Select(e => new RoleAccessScope {AccessScope = e, CreatedAt = DateTime.UtcNow}));
        dbRole.RoleAccessScope.RemoveAll(e => !model.AccessScopes.Contains(e.AccessScope));

        await _mainDb.SaveChangesAsync();

        if (model.Id == 0)
            return;

        var groupIds = await _mainDb.UserRole.Where(e => e.RoleId == model.Id).Select(e => e.GroupId).ToArrayAsync();
        foreach (var id in groupIds)
            await _cache.DeleteKeys(FreverUserPermissionService.GroupCacheKey(id));
    }

    public async Task SaveUserRole(UserRoleModel model)
    {
        await _permissionService.EnsureHasSettingsAccess();

        ArgumentNullException.ThrowIfNull(model);

        var userRoles = await _mainDb.UserRole.Where(e => e.GroupId == model.GroupId).ToArrayAsync();

        var toAdd = model.RoleIds.Where(e => userRoles.All(ur => ur.RoleId != e)).ToArray();
        if (toAdd.Length != 0)
            await _mainDb.UserRole.AddRangeAsync(
                toAdd.Select(e => new UserRole {GroupId = model.GroupId, RoleId = e, CreatedAt = DateTime.UtcNow})
            );

        var toRemove = userRoles.Where(e => !model.RoleIds.Contains(e.RoleId)).ToArray();
        if (toRemove.Length != 0)
            _mainDb.UserRole.RemoveRange(toRemove);

        await _mainDb.SaveChangesAsync();

        await _cache.DeleteKeys(FreverUserPermissionService.GroupCacheKey(model.GroupId));
    }

    public async Task DeleteRole(long id)
    {
        await using var transaction = await _mainDb.BeginTransaction();

        if (await _mainDb.UserRole.AnyAsync(e => e.RoleId == id))
            throw AppErrorWithStatusCodeException.BadRequest("Role is used", "RoleUsed");

        var role = await _mainDb.Role.FindAsync(id);
        if (role is null)
            throw AppErrorWithStatusCodeException.BadRequest("Role not found", "RoleNotFound");

        var userRoles = await _mainDb.RoleAccessScope.Where(e => e.RoleId == id).ToArrayAsync();
        _mainDb.RoleAccessScope.RemoveRange(userRoles);

        _mainDb.Role.Remove(role);

        await _mainDb.SaveChangesAsync();

        await transaction.CommitAsync();
    }

    private static Dictionary<string, AccessScopeDto> GetAllAccessScopes()
    {
        var result = typeof(KnownAccessScopes).GetFields(BindingFlags.Public | BindingFlags.Static)
                                              .Where(f => f.FieldType == typeof(string))
                                              .Select(f => new AccessScopeDto {Name = f.Name, Value = (string) f.GetValue(null)})
                                              .ToArray();

        return result.ToDictionary(e => e.Value);
    }
}