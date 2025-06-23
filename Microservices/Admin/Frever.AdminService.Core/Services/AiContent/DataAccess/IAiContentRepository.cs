using System.Linq;

namespace Frever.AdminService.Core.Services.AiContent.DataAccess;

public interface IAiContentRepository
{
    IQueryable<AiGeneratedContentDto> GetAiGeneratedContent();
}