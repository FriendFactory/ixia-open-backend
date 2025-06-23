using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.AI.Generation.StatusUpdating;

public interface IGeneratedContentUploadingRepository
{
    Task<NestedTransaction> BeginTransaction();
    Task SaveChanges();

    Task<AiGeneratedContent> GetAiGeneratedContentById(long id);
    Task<AiGeneratedContent> GetAiGeneratedContentByPartialName(string partialName);
    Task<AiGeneratedImage> GetAiGeneratedImageById(long id, long groupId);
    Task<AiGeneratedVideo> GetAiGeneratedVideoById(long id, long groupId);
    Task<AiGeneratedVideoClip[]> GetAiGeneratedVideoClips(long id);
}

public class GeneratedContentUploadingRepository(IWriteDb db) : IGeneratedContentUploadingRepository
{
    public Task<NestedTransaction> BeginTransaction()
    {
        return db.BeginTransactionSafe();
    }

    public Task SaveChanges()
    {
        return db.SaveChangesAsync();
    }

    public Task<AiGeneratedContent> GetAiGeneratedContentById(long id)
    {
        return db.AiGeneratedContent.FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<AiGeneratedContent> GetAiGeneratedContentByPartialName(string partialName)
    {
        return db.AiGeneratedContent.FirstOrDefaultAsync(e => e.GenerationKey.Contains(partialName));
    }

    public Task<AiGeneratedImage> GetAiGeneratedImageById(long id, long groupId)
    {
        return db.AiGeneratedContent.Where(e => e.Id == id && e.GroupId == groupId)
                 .Join(db.AiGeneratedImage, e => e.AiGeneratedImageId, i => i.Id, (e, i) => i)
                 .FirstOrDefaultAsync();
    }

    public Task<AiGeneratedVideo> GetAiGeneratedVideoById(long id, long groupId)
    {
        return db.AiGeneratedContent.Where(e => e.Id == id && e.GroupId == groupId)
                 .Join(db.AiGeneratedVideo, e => e.AiGeneratedVideoId, i => i.Id, (e, i) => i)
                 .FirstOrDefaultAsync();
    }

    public Task<AiGeneratedVideoClip[]> GetAiGeneratedVideoClips(long id)
    {
        return db.AiGeneratedVideoClip.Where(e => e.AiGeneratedVideoId == id).ToArrayAsync();
    }
}