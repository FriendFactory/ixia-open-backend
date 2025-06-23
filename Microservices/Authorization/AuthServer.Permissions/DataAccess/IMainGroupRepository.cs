using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace AuthServer.Permissions.DataAccess;

public interface IMainGroupRepository
{
    IQueryable<Group> FindGroupById(long groupId);

    Task SetGroupBlocked(long groupId, bool isBlocked);

    Task SetGroupDeleted(long groupId, DateTime? deletedAt);

    IQueryable<Country> FindCountryByCode(string isoCode);

    IQueryable<Country> FindCountryById(long countryId);

    Task<Role[]> GetUserRoles(long groupId);
}