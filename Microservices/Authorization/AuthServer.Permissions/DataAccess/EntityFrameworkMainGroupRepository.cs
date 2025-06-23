using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Permissions.DataAccess;

public class EntityFrameworkMainGroupRepository(IWriteDb writeDb) : IMainGroupRepository
{
    public IQueryable<Group> FindGroupById(long groupId)
    {
        return writeDb.Group.Where(e => e.Id == groupId);
    }

    public async Task SetGroupBlocked(long groupId, bool isBlocked)
    {
        var group = await FindGroupById(groupId).FirstOrDefaultAsync();

        if (group == null)
            throw AppErrorWithStatusCodeException.BadRequest("Group is not found", "GroupNotFound");

        group.IsBlocked = isBlocked;

        await writeDb.SaveChangesAsync();
    }

    public async Task SetGroupDeleted(long groupId, DateTime? deletedAt)
    {
        var group = await FindGroupById(groupId).FirstOrDefaultAsync();

        if (group == null)
            throw AppErrorWithStatusCodeException.BadRequest("Group is not found", "GroupNotFound");

        group.DeletedAt = deletedAt;

        await writeDb.SaveChangesAsync();
    }

    public IQueryable<Country> FindCountryByCode(string isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(isoCode));

        isoCode = isoCode.Trim().ToLowerInvariant();

        return writeDb.Country.Where(c => c.ISO2Code == isoCode || c.ISOName == isoCode);
    }

    public IQueryable<Country> FindCountryById(long countryId)
    {
        return writeDb.Country.Where(c => c.Id == countryId);
    }

    public Task<Role[]> GetUserRoles(long groupId)
    {
        return writeDb.Role.Where(e => writeDb.UserRole.Any(u => u.GroupId == groupId && u.RoleId == e.Id))
                      .Include(e => e.RoleAccessScope)
                      .ToArrayAsync();
    }
}