using System.Collections.Generic;
using System.Threading.Tasks;
using Frever.ClientService.Contract.Metadata;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.AI.Metadata;

public interface IAiMetadataService
{
    Task<MetadataDto> GetMetadata();
    Task<ArtStyleDto[]> GetArtStyles(long? genderId, int skip, int take);
    Task<MakeUpDto[]> GetMakeUps(long? categoryId, int skip, int take);
    Task<PromptDataDto> GetLlmPromptsData(PromptInput input);
    Task<SpeakerModeDto[]> GetSpeakerModes(int skip, int take);
    Task<LanguageModeDto[]> GetLanguageModes(int skip, int take);
    Task<AiArtStyle> GetArtStyleByIdInternal(long id);
    Task<AiMakeUp> GetMakeUpByIdInternal(long id);
    Task<AiSpeakerMode> GetSpeakerModeOrDefaultInternal(long? speakerModeId);
    Task<AiLanguageMode> GetLanguageModeOrDefaultInternal(long? languageModeId);
    Task<Dictionary<long, AiArtStyle>> GetArtStyleByIdsInternal(IEnumerable<long> ids);
    Task<string> GetPopulatedPromptInternal(string key, Dictionary<string, string> parameters);
}