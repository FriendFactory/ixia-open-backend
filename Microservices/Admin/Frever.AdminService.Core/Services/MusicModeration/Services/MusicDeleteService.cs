using System;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using Common.Infrastructure.Caching;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.AdminService.Core.Services.MusicModeration.Services;

public interface IMusicDeleteService
{
    Task SetDeleteContentBySongId(long songId, bool isDeleted);
    Task SetDeletedContentByUserSoundId(long userSoundId, bool isDeleted);
    Task SetDeletedContentByExternalSongId(long externalSongId, bool isDeleted, bool clearCache = true);
}

public class MusicDeleteService(IWriteDb db, ICache cache, IUserPermissionService permissionService, ILogger<MusicDeleteService> log)
    : IMusicDeleteService
{
    public async Task SetDeleteContentBySongId(long songId, bool isDeleted)
    {
        await permissionService.EnsureHasCategoryFullAccess();

        log.LogInformation("SetDeleteContentBySongId(songId={SongId} isDeleted={IsDeleted})", songId, isDeleted);

        await using var transaction = await db.BeginTransaction();

        var song = await db.Song.FindAsync(songId);
        if (song != null)
        {
            song.ReadinessId = (await GetReadinessForStatus(isDeleted)).Id;
            await db.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        await cache.ClearCache();
    }

    public async Task SetDeletedContentByUserSoundId(long userSoundId, bool isDeleted)
    {
        await permissionService.EnsureHasCategoryFullAccess();

        log.LogInformation("SetDeletedContentByUserSoundId(userSoundId={UserSoundId} isDeleted={IsDeleted})", userSoundId, isDeleted);

        await using var transaction = await db.BeginTransaction();

        var userSound = await db.UserSound.FindAsync(userSoundId);
        if (userSound != null)
        {
            userSound.DeletedAt = isDeleted ? DateTime.UtcNow : null;
            await db.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        await cache.ClearCache();
    }

    public async Task SetDeletedContentByExternalSongId(long externalSongId, bool isDeleted, bool clearCache = true)
    {
        await permissionService.EnsureHasCategoryFullAccess();

        log.LogInformation(
            "DeleteContentByExternalSongId(externalSongId={ExternalSongId} isDeleted={IsDeleted})",
            externalSongId,
            isDeleted
        );

        await using var transaction = await db.BeginTransaction();

        var externalSong = await db.ExternalSongs.FindAsync(externalSongId);
        if (externalSong != null)
        {
            externalSong.IsDeleted = isDeleted;
            await db.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        if (clearCache)
            await cache.ClearCache();
    }

    private async Task<Readiness> GetReadinessForStatus(bool isDeleted)
    {
        if (isDeleted)
            return await db.Readiness.FirstOrDefaultAsync(r => r.Name == "Discontinued") ??
                   await db.Readiness.FirstOrDefaultAsync(r => r.Id == Readiness.KnownReadinessReady - 1);

        return await db.Readiness.FirstOrDefaultAsync(r => r.Id == Readiness.KnownReadinessReady) ??
               throw new InvalidOperationException("No Ready readiness");
    }
}