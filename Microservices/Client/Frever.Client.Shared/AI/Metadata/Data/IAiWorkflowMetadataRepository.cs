using System.Linq;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Shared.AI.Metadata.Data;

public interface IAiWorkflowMetadataRepository
{
    IQueryable<AiWorkflowMetadata> GetAiWorkflowMetadata();
}