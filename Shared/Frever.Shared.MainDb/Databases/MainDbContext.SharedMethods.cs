using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Frever.Shared.MainDb;

public partial class MainDbContext
{
    public IQueryable<GroupWithAge> GetGroupWithAgeInfo(long groupId, long geoClusterId)
    {
        var sql = $"""
                   with available_groups as (select *
                                             from "Group" g
                                             where g."IsBlocked" = false
                                               and g."DeletedAt" is null
                                               and g."IsTemporary" = false
                                               and g."NickName" not like '1test%'
                                               and not exists (select 1
                                                               from "BlockedUser" bu
                                                               where bu."BlockedUserId" = {groupId}
                                                                 and bu."BlockedByUserId" = g."Id")
                                               and not exists (select 1
                                                               from "BlockedUser" bu
                                                               where bu."BlockedByUserId" = {groupId}
                                                                 and bu."BlockedUserId" = g."Id")),
                        recently_logged_in_groups as (select *
                                                      from available_groups ag
                                                      where exists(select 1
                                                                   from "UserActivity" a
                                                                   where a."ActionType" = 'Login'
                                                                     and a."GroupId" = ag."Id")),
                        active_groups as (select a.*
                                          from recently_logged_in_groups a
                                                   inner join
                                               "User" u on a."Id" = u."MainGroupId"
                                          where a."TotalFollowers" > 0
                                            and u."MainCharacterId" is not null),
                        same_geo_cluster_groups as (select g.*
                                                    from active_groups g
                                                             inner join
                                                         "Country" c on g."TaxationCountryId" = c."Id"
                                                             inner join
                                                         "Language" l on g."DefaultLanguageId" = l."Id"
                                                             cross join "GeoCluster" gc
                                                    where gc."Id" = {geoClusterId}
                                                      and ((gc."ShowToUserFromCountry" = ARRAY['*'] or
                                                            c."ISOName" = ANY (gc."ShowToUserFromCountry")) and
                                                           (gc."ShowForUserWithLanguage" = ARRAY['*'] or
                                                            l."IsoCode" = ANY (gc."ShowForUserWithLanguage")) and
                                                           (gc."HideForUserFromCountry" = ARRAY[]::text[] or not (
                                                               c."ISOName" = ANY (gc."HideForUserFromCountry")
                                                               )) and
                                                           (gc."HideForUserWithLanguage" = ARRAY[]::text[] or not (
                                                               l."IsoCode" = ANY (gc."HideForUserWithLanguage")
                                                               ))
                                                        ))
                   select g."Id" as "GroupId",
                          abs(extract(day from g."BirthDate"::timestamp - self."BirthDate"::timestamp)) "AgeDiff"
                   from same_geo_cluster_groups g
                            cross join
                            (select * from "Group" g1 where g1."Id" = {groupId}) self
                   """;

        return GroupWithAge.FromSqlRaw(sql);
    }
}