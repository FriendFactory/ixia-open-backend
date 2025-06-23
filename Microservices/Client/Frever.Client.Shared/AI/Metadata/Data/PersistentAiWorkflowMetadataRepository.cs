using System.Linq;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Shared.AI.Metadata.Data;

public class PersistentAiWorkflowMetadataRepository(IWriteDb db) : IAiWorkflowMetadataRepository
{
    public IQueryable<AiWorkflowMetadata> GetAiWorkflowMetadata()
    {
        return db.AiWorkflowMetadata;
    }
}