using System.Linq;
using System.Threading.Tasks;
using Frever.Client.Shared.AI.Metadata.Data;
using Frever.ClientService.Contract.Metadata;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;

namespace Frever.Client.Shared.AI.Metadata;

public interface IAiWorkflowMetadataService
{
    Task<AiWorkflowMetadataInfo[]> Get();
    Task<AiWorkflowMetadata[]> GetInternal();
}

public class AiWorkflowMetadataService(IAiWorkflowMetadataRepository repo) : IAiWorkflowMetadataService
{
    public async Task<AiWorkflowMetadataInfo[]> Get()
    {
        var all = await GetInternal();
        return all.Select(
                       e => new AiWorkflowMetadataInfo
                            {
                                Id = e.Id,
                                Key = e.Key,
                                UnitPrice = e.HardCurrencyPrice,
                                EstimatedLoadingTimeSec = e.EstimatedLoadingTimeSec
                            }
                   )
                  .ToArray();
    }

    public Task<AiWorkflowMetadata[]> GetInternal()
    {
        return repo.GetAiWorkflowMetadata().Where(p => p.IsActive).AsNoTracking().ToArrayAsync();
    }
}