using System.Collections.Generic;

namespace Frever.AdminService.Core.Services.EntityServices;

public class EntityDeletionMarkFeature<TEntity>
{
    private readonly HashSet<long> _deleteEntityIds = [];

    public EntityDeletionMarkFeature<TEntity> MarkForDeletion(long entityId)
    {
        _deleteEntityIds.Add(entityId);

        return this;
    }

    public EntityDeletionMarkFeature<TEntity> CancelDeletion(long entityId)
    {
        _deleteEntityIds.Remove(entityId);

        return this;
    }

    public bool ShouldDeleteEntity(long entityId)
    {
        return _deleteEntityIds.Contains(entityId);
    }
}