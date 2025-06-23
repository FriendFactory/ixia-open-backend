using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.AI.Generation;

public interface IGenerationRepository
{
    Task<Song> GetSongById(long id);
    Task<UserSound> GetUserSoundById(long id);
    Task<ExternalSong> GetExternalSongById(long id);
    Task<AiGeneratedImage> GetAiGeneratedImageById(long id, long groupId);
    Task<AiGeneratedVideo> GetAiGeneratedVideoById(long id, long groupId);
    IQueryable<AiGeneratedVideoClip> GetAiGeneratedVideoClip(long videoId);
}

public sealed class GenerationRepository(IWriteDb db) : IGenerationRepository
{
    public Task<Song> GetSongById(long id)
    {
        return db.Song.FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<UserSound> GetUserSoundById(long id)
    {
        return db.UserSound.FirstOrDefaultAsync(e => e.Id == id && e.DeletedAt == null);
    }

    public Task<ExternalSong> GetExternalSongById(long id)
    {
        return db.ExternalSongs.FirstOrDefaultAsync(e => e.Id == id && !e.IsManuallyDeleted && !e.IsDeleted && e.NotClearedSince == null);
    }

    public Task<AiGeneratedImage> GetAiGeneratedImageById(long id, long groupId)
    {
        return db.AiGeneratedContent.Where(e => e.Id == id && e.GroupId == groupId)
                 .Where(e => e.DeletedAt == null && e.GenerationStatus == AiGeneratedContent.KnownGenerationStatusCompleted)
                 .Join(db.AiGeneratedImage, e => e.AiGeneratedImageId, i => i.Id, (e, i) => i)
                 .FirstOrDefaultAsync();
    }

    public Task<AiGeneratedVideo> GetAiGeneratedVideoById(long id, long groupId)
    {
        return db.AiGeneratedContent.Where(e => e.Id == id && e.GroupId == groupId)
                 .Where(e => e.DeletedAt == null && e.GenerationStatus == AiGeneratedContent.KnownGenerationStatusCompleted)
                 .Join(db.AiGeneratedVideo, e => e.AiGeneratedVideoId, i => i.Id, (e, i) => i)
                 .FirstOrDefaultAsync();
    }

    public IQueryable<AiGeneratedVideoClip> GetAiGeneratedVideoClip(long videoId)
    {
        return db.AiGeneratedVideoClip.Where(e => e.AiGeneratedVideoId == videoId).AsNoTracking();
    }
}