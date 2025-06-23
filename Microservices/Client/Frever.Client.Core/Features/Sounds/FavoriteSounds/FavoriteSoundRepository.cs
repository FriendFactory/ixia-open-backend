using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using AuthServerShared;
using Common.Infrastructure.Sounds;
using Common.Models;
using Frever.Client.Core.Utils;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.Sounds.FavoriteSounds;

public interface IFavoriteSoundRepository
{
    IQueryable<FavoriteSound> GetSoundsByGroupId(long groupId);
    IQueryable<FavoriteSoundInternal> GetFavoriteSoundInternalQuery(UserInfo userInfo);
    IQueryable<FavoriteSoundInternal> GetFavoriteSoundInternalQueryById(long id, FavoriteSoundType type, UserInfo userInfo);
    IQueryable<Frever.Shared.MainDb.Entities.Song> GetSongByLabelId(long id);
    Task<long[]> GetFavoriteSongIds(long groupId, IEnumerable<long> ids);
    Task<long[]> GetFavoriteUserSoundIds(long groupId, IEnumerable<long> ids);
    Task<long[]> GetFavoriteExternalSongIds(long groupId, IEnumerable<long> ids);
    Task AddFavoriteSound(FavoriteSound sound);
    Task RemoveFavoriteSound(FavoriteSound sound);
}

internal sealed class FavoriteSoundRepository(IWriteDb writeDb) : IFavoriteSoundRepository
{
    public IQueryable<FavoriteSound> GetSoundsByGroupId(long groupId)
    {
        return writeDb.FavoriteSound.Where(e => e.GroupId == groupId);
    }

    public IQueryable<Frever.Shared.MainDb.Entities.Song> GetSongByLabelId(long id)
    {
        return writeDb.Song.OrderBy(e => e.SortOrder).Where(e => e.LabelId == id).AccessibleForEveryone().AsNoTracking();
    }

    public Task<long[]> GetFavoriteSongIds(long groupId, IEnumerable<long> ids)
    {
        return GetSoundsByGroupId(groupId)
              .Where(e => e.SongId.HasValue)
              .Where(e => ids.Contains(e.SongId.Value))
              .Select(e => e.SongId.Value)
              .ToArrayAsync();
    }

    public Task<long[]> GetFavoriteUserSoundIds(long groupId, IEnumerable<long> ids)
    {
        return GetSoundsByGroupId(groupId)
              .Where(e => e.UserSoundId.HasValue)
              .Where(e => ids.Contains(e.UserSoundId.Value))
              .Select(e => e.UserSoundId.Value)
              .ToArrayAsync();
    }

    public Task<long[]> GetFavoriteExternalSongIds(long groupId, IEnumerable<long> ids)
    {
        return GetSoundsByGroupId(groupId)
              .Where(e => e.ExternalSongId.HasValue)
              .Where(e => ids.Contains(e.ExternalSongId.Value))
              .Select(e => e.ExternalSongId.Value)
              .ToArrayAsync();
    }

    public async Task AddFavoriteSound(FavoriteSound sound)
    {
        ArgumentNullException.ThrowIfNull(sound);

        sound.Time = DateTime.UtcNow;

        await writeDb.FavoriteSound.AddAsync(sound);
        await writeDb.SaveChangesAsync();
    }

    public async Task RemoveFavoriteSound(FavoriteSound sound)
    {
        ArgumentNullException.ThrowIfNull(sound);

        writeDb.FavoriteSound.Remove(sound);

        await writeDb.SaveChangesAsync();
    }

    public IQueryable<FavoriteSoundInternal> GetFavoriteSoundInternalQuery(UserInfo userInfo)
    {
        var sql = $"""
                      {FavoriteSoundInternalQuery(userInfo)}
                      and (s."Id" is not null or es."ExternalTrackId" is not null or us."Id" is not null)
                   """;

        return writeDb.SqlQueryRaw<FavoriteSoundInternal>(sql);
    }

    public IQueryable<FavoriteSoundInternal> GetFavoriteSoundInternalQueryById(long id, FavoriteSoundType type, UserInfo userInfo)
    {
        var fieldSql = type switch
                       {
                           FavoriteSoundType.Song => "fs.\"SongId\"",
                           FavoriteSoundType.ExternalSong => "fs.\"ExternalSongId\"",
                           FavoriteSoundType.UserSound => "fs.\"UserSoundId\"",
                           _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                       };

        var sql = $"""
                      {FavoriteSoundInternalQuery(userInfo)}
                      and {fieldSql} = {id}
                   """;

        return writeDb.SqlQueryRaw<FavoriteSoundInternal>(sql);
    }

    private static string FavoriteSoundInternalQuery(UserInfo userInfo)
    {
        var sql = $"""
                   select
                       COALESCE(s."Id", es."ExternalTrackId", us."Id") AS "Id",
                       COALESCE(s."Name", es."SongName", us."Name") AS "SongName",
                       COALESCE(s."ArtistName", es."ArtistName") AS "ArtistName",
                       COALESCE(s."Duration", us."Duration", 0) AS "Duration",
                       COALESCE(s."Files", us."Files") AS "Files",
                       COALESCE(s."UsageCount", us."UsageCount", es."UsageCount") AS "UsageCount",
                       us."GroupId" as "OwnerGroupId",
                       case when s."Id" is not null then {(int) FavoriteSoundType.Song}
                           when es."ExternalTrackId" is not null then {(int) FavoriteSoundType.ExternalSong}
                           else {(int) FavoriteSoundType.UserSound} end as "Type",
                       fs."Time"
                   from "FavoriteSound" fs
                       left join (
                                     select s."Id", a."Name" as "ArtistName", s."Duration", s."Files", s."Name", s."UsageCount"
                                     from "Song" s
                                     join "Artist" a on s."ArtistId" = a."Id"
                                     where s."GroupId" = {Constants.PublicAccessGroupId}
                                         {ReadyForUserRoleFilter("s", userInfo)}
                                 ) s on fs."SongId" = s."Id"
                       left join (
                                     select us."Id", us."Duration", us."Files", us."GroupId", us."Name", us."UsageCount"
                                     from "UserSound" us
                                     where us."DeletedAt" is null
                                        or exists (select 1
                                                   from "MusicController" m
                                                       inner join "Event" e on m."EventId" = e."Id"
                                                       inner join "Level" l on e."LevelId" = l."Id"
                                                   where us."Id" = m."UserSoundId"
                                                     and not (l."IsDeleted")
                                                     and not (l."IsDraft"))
                                 ) us on fs."UserSoundId" = us."Id"
                       left join (
                                     select es."ExternalTrackId", es."ArtistName", es."SongName", es."UsageCount"
                                     from "ExternalSong" es
                                     where not (es."IsManuallyDeleted")
                                       and not (es."IsDeleted")
                                       and es."NotClearedSince" is null
                                 ) es on fs."ExternalSongId" = es."ExternalTrackId"
                   where fs."GroupId" = {userInfo.UserMainGroupId}
                   """;

        return sql;
    }

    private static string ReadyForUserRoleFilter(string sqlName, UserInfo userInfo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sqlName);

        if (userInfo.AccessScopes.Contains(KnownAccessScopes.ReadinessFull))
            return string.Empty;

        var isArtistSql = userInfo.AccessScopes.Contains(KnownAccessScopes.ReadinessArtists)
                              ? $"""or {sqlName}."ReadinessId" < 10"""
                              : string.Empty;

        var creatorAccessLevelsSql = userInfo.CreatorAccessLevels.Length > 0
                                         ? $"""or {sqlName}."ReadinessId" in ({string.Join(',', userInfo.CreatorAccessLevels)})"""
                                         : string.Empty;

        var result = $""" and ({sqlName}."ReadinessId" = {Readiness.KnownReadinessReady} {isArtistSql} {creatorAccessLevelsSql}) """;

        return result;
    }
}