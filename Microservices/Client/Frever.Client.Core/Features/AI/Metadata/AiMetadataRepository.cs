using System.Linq;
using System.Threading.Tasks;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Core.Features.AI.Metadata;

public interface IAiMetadataRepository
{
    Task<AiSpeakerMode[]> GetAiSpeakerModes();
    Task<AiLanguageMode[]> GetAiLanguageModes();
    Task<AiArtStyle[]> GetAiArtStyles();
    Task<AiLlmPrompt[]> GetAiLlmPrompts();
    IQueryable<Gender> GetGenders();
    Task<AiMakeUp[]> GetMakeUps();
}

public class AiMetadataRepository(IWriteDb writeDb) : IAiMetadataRepository
{
    public Task<AiSpeakerMode[]> GetAiSpeakerModes()
    {
        return writeDb.AiSpeakerMode.AsNoTracking().ToArrayAsync();
    }

    public Task<AiLanguageMode[]> GetAiLanguageModes()
    {
        return writeDb.AiLanguageMode.AsNoTracking().ToArrayAsync();
    }

    public Task<AiArtStyle[]> GetAiArtStyles()
    {
        return writeDb.AiArtStyle.Where(e => e.IsEnabled).AsNoTracking().ToArrayAsync();
    }

    public Task<AiLlmPrompt[]> GetAiLlmPrompts()
    {
        return writeDb.AiLlmPrompt.AsNoTracking().ToArrayAsync();
    }

    public IQueryable<Gender> GetGenders()
    {
        return writeDb.Gender.Where(e => e.IsEnabled).AsNoTracking();
    }

    public Task<AiMakeUp[]> GetMakeUps()
    {
        return writeDb.AiMakeUp.Include(e => e.Category).Where(e => e.IsEnabled).AsNoTracking().ToArrayAsync();
    }
}