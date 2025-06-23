using System.Threading.Tasks;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.ClientService.Contract.Ai;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Content;

public interface IAiGeneratedContentService
{
    Task<AiGeneratedContentStatusDto> GetStatus(long id);

    Task<AiGeneratedContentShortInfo[]> GetDrafts(AiGeneratedContentType? type, int skip, int take);

    Task<AiGeneratedContentShortInfo[]> GetFeed(long groupId, AiGeneratedContentType? type, int skip, int take);

    Task<AiGeneratedContentFullInfo> GetById(long id);

    Task<AiGeneratedContentFullInfo> SaveDraft(AiGeneratedContentInput input);

    Task<AiGeneratedContentFullInfo> Publish(long aiGeneratedContentId);

    Task Delete(long id);

    Task<long> SaveDraftInternal(AiGenerationInput input, bool hideResult = false);

    Task SetGenerationInfo(long id, string generationKey, AiContentGenerationParameters parameters);
}