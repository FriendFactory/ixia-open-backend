using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthServerShared;
using Common.Infrastructure.Services;
using Common.Models.Database.Interfaces;
using FluentValidation;
using Frever.AdminService.Core.Services.ModelSettingsProviders;
using Frever.AdminService.Core.UoW;
using Frever.AdminService.Core.Utils;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Frever.AdminService.Core.Services.EntityServices;

public partial class DefaultEntityWriteAlgorithm<TEntity>(
    UserInfo user,
    IEnumerable<IEntityValidator<TEntity>> validators,
    ILoggerFactory loggerFactory,
    AssetGroupProvider assetGroupProvider,
    IUnitOfWork unitOfWork,
    IEntityReadAlgorithm<TEntity> readAlgorithm,
    IEntityLifeCycle<TEntity> lifeCycle
) : IEntityWriteAlgorithm<TEntity>
    where TEntity : class, IEntity
{
    private readonly List<Dependency> _dependencies = [];
    private readonly IEnumerable<IEntityValidator<TEntity>> _validators = validators ?? [];

    protected readonly IUnitOfWork UnitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    protected readonly UserInfo User = user ?? throw new ArgumentNullException(nameof(user));
    protected readonly IEntityReadAlgorithm<TEntity> ReadAlgorithm = readAlgorithm ?? throw new ArgumentNullException(nameof(readAlgorithm));
    protected readonly AssetGroupProvider AssetGroupProvider = assetGroupProvider ?? throw new ArgumentNullException(nameof(assetGroupProvider));

    protected readonly ILogger Logger = loggerFactory.CreateLogger($"Frever.AdminService.Core.WriteEntityService[{typeof(TEntity).Name}]");

    public virtual async Task ModifyInputEntity(TEntity entity, CallContext context)
    {
        using var scope = Logger.BeginScope($"ModifyInputEntity(entity={entity.GetType()})");

        if (entity is ITaggable {Tags: null} taggable)
            taggable.Tags = [];

        if (context.Operation == WriteOperation.Update && entity is ITimeChangesTrackable trackable)
        {
            var createdTime = await ReadAlgorithm.GetOne(entity.Id)
                                                 .Cast<ITimeChangesTrackable>()
                                                 .Select(e => e.CreatedTime)
                                                 .FirstOrDefaultAsync();

            trackable.CreatedTime = createdTime;
        }

        await _dependencies.Select(dep => dep.ModifyInputEntity(entity, context)).Collapse();
    }

    public virtual Task<ValidationResult> Validate(TEntity entity, CallContext context)
    {
        using var scope = Logger.BeginScope($"Validate(entity={entity.GetType()})");

        return _validators.Select(v => v.Validate(entity, context))
                          .Concat(_dependencies.Select(d => d.Validate(entity, context)))
                          .Collapse((acc, result1) => acc.Compose(result1), ValidationResult.Valid);
    }

    public virtual async Task<bool> CanSave(TEntity entity, CallContext context)
    {
        using var scope = Logger.BeginScope($"CanSave(entity={entity.GetType()})");

        if (entity.Id == 0)
        {
            Logger.LogDebug("Entity has ID=0, readability check skipped");
        }
        else if (!await ReadAlgorithm.Any(entity.Id))
        {
            Logger.LogDebug("Entity ID={EntityId} is not readable, saving prohibited", entity.Id);

            return false;
        }

        var result = await _dependencies.Select(d => d.CanSave(entity, context)).Collapse((acc, result) => acc && result, true);

        return result;
    }

    public virtual async Task PreSave(TEntity entity, CallContext context)
    {
        using var scope = Logger.BeginScope($"PreSave(entity={entity.GetType()})");

        if (entity is IStageable s && s.ReadinessId == 0)
            s.ReadinessId = 1;

        if (context.Operation == WriteOperation.Create)
        {
            if (entity is IGroupAccessible groupAccessible)
                groupAccessible.GroupId = AssetGroupProvider.GetAssetGroup(entity.GetType(), User.UserMainGroupId);

            if (entity is IUploadedByUser uploadedByUser)
                uploadedByUser.UploaderUserId = User.UserId;
        }

        if (entity is IUpdatedByUser updatedByUser)
            updatedByUser.UpdatedByUserId = User.UserId;

        await _dependencies.Select(dep => dep.PreSave(entity, context)).Collapse();
    }

    /// <summary>
    ///     Marks entity as deleted.
    ///     NOTE dependencies are not automatically deleted.
    ///     To delete dependencies call DeleteDependencies method.
    /// </summary>
    public virtual async Task MarkForDeletion(TEntity entity, CallContext context)
    {
        using var scope = Logger.BeginScope($"MarkForDeletion(entity={entity.GetType()})");

        if (entity.Id == 0)
            throw new ValidationException($"Entity {typeof(TEntity).Name} should have ID to be deleted");

        context.ModifyFeature((EntityDeletionMarkFeature<TEntity> f) => f.MarkForDeletion(entity.Id));

        var contextEntity = await ReadAlgorithm.GetForDeletion(entity.Id).SingleAsyncSafe();
        UnitOfWork.MarkAsDeleted(contextEntity);
        Logger.LogDebug("Mark entity as deleted");

        var entityToDeleteWithDependencies = await GetEntityWithDependenciesForDeletion(entity);
        foreach (var dep in _dependencies.Where(d => d.DeleteDependentEntities))
            await dep.MarkForDeletion(entityToDeleteWithDependencies, context);
    }

    public virtual async Task CancelEntityDeletion(TEntity entity, CallContext context)
    {
        using var scope = Logger.BeginScope($"CancelEntityDeletion(entity={entity.GetType()})");

        var entityToDeleteWithDependencies = await GetEntityWithDependenciesForDeletion(entity);

        UnitOfWork.ClearDeletionMark(entityToDeleteWithDependencies);
        context.ModifyFeature((EntityDeletionMarkFeature<TEntity> f) => f.CancelDeletion(entityToDeleteWithDependencies.Id));

        foreach (var dep in _dependencies.Where(d => d.DeleteDependentEntities))
            await dep.CancelEntityDeletion(entityToDeleteWithDependencies, context);
    }

    public virtual Task AfterSaveChanges(TEntity entity, CallContext context)
    {
        using var scope = Logger.BeginScope($"AfterSaveChanges(entity={entity.GetType()})");

        var result = _dependencies.Select(d => d.AfterSaveChanges(entity, context)).Collapse();

        return result;
    }

    /// <summary>
    ///     Called after data have successfully written to database, so all entities got their IDs etc.
    /// </summary>
    public virtual async Task AfterCommit(TEntity entity, CallContext context)
    {
        using var scope = Logger.BeginScope($"AfterCommit(entity={entity.GetType()})");

        await _dependencies.Select(d => d.AfterCommit(entity, context)).Collapse();
        switch (context.Operation)
        {
            case WriteOperation.Create:
                await lifeCycle.OnCreated(entity);
                break;
            case WriteOperation.Update:
                await lifeCycle.OnUpdated(entity);
                break;
            case WriteOperation.Delete:
                await lifeCycle.OnDeleted(entity);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Dispose()
    {
        UnitOfWork?.Dispose();
    }

    /// <summary>
    ///     This method is required because due deletion we need to load all entity dependencies in
    ///     order to execute deletion logic on them.
    /// </summary>
    protected virtual async Task<TEntity> GetEntityWithDependenciesForDeletion(TEntity entity)
    {
        var entityToDeleteQuery = ReadAlgorithm.GetForDeletion(entity.Id);

        entityToDeleteQuery = _dependencies.Where(d => d.DeleteDependentEntities)
                                           .Aggregate(entityToDeleteQuery, (current, d) => current.Include(d.NavigationPropertyPath));

        var entityToDelete = await entityToDeleteQuery.SingleOrDefaultAsyncSafe();

        if (entityToDelete == null)
            throw new EntityWriteException($"{typeof(TEntity).Name} with ID={entity.Id} is not found", WriteOperation.Delete, 404);

        return entityToDelete;
    }

    protected bool IsEntityMarkedForDeletion(TEntity entity, CallContext context)
    {
        var result = context.GetFeature<EntityDeletionMarkFeature<TEntity>>().ShouldDeleteEntity(entity.Id);

        return result;
    }
}