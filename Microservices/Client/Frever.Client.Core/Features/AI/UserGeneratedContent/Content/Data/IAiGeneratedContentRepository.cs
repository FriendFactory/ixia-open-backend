using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Data;

public interface IAiGeneratedContentRepository
{
    IQueryable<AiGeneratedContentFullData> GetDraftList(long groupId);
    IQueryable<AiGeneratedContentFullData> GetPublishedList(long currentGroupId, long groupId);
    IQueryable<AiGeneratedContent> GetById(long id);

    IQueryable<AiGeneratedImage> GetOwnAiImageById(long id, long groupId);
    IQueryable<AiGeneratedImage> GetAiImageById(long id);
    IQueryable<AiGeneratedImagePerson> GetAiImagePerson(long aiGeneratedImageId);
    IQueryable<AiGeneratedImageSource> GetAiImageSources(long aiGeneratedImageId);
    IQueryable<AiCharacterImage> GetAiCharacterImage(long aiCharacterImageId);

    IQueryable<AiGeneratedVideo> GetOwnAiVideoById(long id, long groupId);
    IQueryable<AiGeneratedVideo> GetAiVideoById(long id);
    IQueryable<AiGeneratedVideoClip> GetAiVideoClips(long aiGeneratedVideoId);

    Task<AiGeneratedImage> GetContentImage(long aiContentId, long groupId);

    TEntity Add<TEntity>(TEntity entity)
        where TEntity : class;

    IEnumerable<TEntity> AddRange<TEntity>(IEnumerable<TEntity> entity)
        where TEntity : class;

    TEntity Remove<TEntity>(TEntity entity)
        where TEntity : class;

    Task DeleteVideoByAiContentId(long aiContentId);

    Task<int> SaveChanges();

    Task<NestedTransaction> BeginTransaction();
    IQueryable<AiGeneratedContentFullData> GetOwnAiContent(long groupId);
}

public class AiGeneratedContentFullData
{
    public AiGeneratedContent Content { get; set; }
    public AiGeneratedImage Image { get; set; }
    public AiGeneratedVideo Video { get; set; }
    public Group Group { get; set; }
    public bool IsPublished { get; set; }
}