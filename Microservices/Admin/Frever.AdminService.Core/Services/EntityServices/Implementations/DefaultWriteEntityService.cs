using System;
using System.Threading.Tasks;
using AuthServer.Permissions.Services;
using Common.Models.Database.Interfaces;
using Frever.AdminService.Core.UoW;
using Frever.AdminService.Core.Utils;
using Microsoft.Extensions.Logging;

namespace Frever.AdminService.Core.Services.EntityServices;

internal class DefaultWriteEntityService<TEntity>(
    IEntityReadAlgorithm<TEntity> readAlgorithm,
    ILoggerFactory loggerFactory,
    IEntityWriteAlgorithm<TEntity> writeAlgorithm,
    IUnitOfWork unitOfWork,
    IUserPermissionService permissionService
) : IWriteEntityService<TEntity>
    where TEntity : class, IEntity
{
    private readonly IUserPermissionService _permissionService =
        permissionService ?? throw new ArgumentNullException(nameof(permissionService));

    protected IEntityReadAlgorithm<TEntity> ReadAlgorithm { get; } = readAlgorithm ?? throw new ArgumentNullException(nameof(readAlgorithm));
    protected IEntityWriteAlgorithm<TEntity> WriteAlgorithm { get; } = writeAlgorithm ?? throw new ArgumentNullException(nameof(writeAlgorithm));
    protected ILogger Log { get; } = loggerFactory.CreateLogger($"Frever.AdminService.Core.WriteEntityService[{typeof(TEntity).Name}]");
    protected IUnitOfWork UnitOfWork { get; } = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public virtual async Task<TEntity> Create(TEntity entity, object extraData = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var scope = Log.BeginScope(this);

        Log.LogInformation("Entity creation started");

        var context = CreateCallContext(entity, WriteOperation.Create);

        var result = await WriteCore(entity, context);

        return result;
    }

    public virtual async Task<TEntity> Update(TEntity entity, object extraData = null)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (typeof(TEntity).IsAssignableTo(typeof(IAdminAsset)))
            await _permissionService.EnsureHasAssetFullAccess();

        if (typeof(TEntity).IsAssignableTo(typeof(IAdminCategory)))
            await _permissionService.EnsureHasCategoryFullAccess();

        using var scope = Log.BeginScope(this);

        Log.LogInformation("Entity ID={EntityId} started", entity.Id);

        var context = CreateCallContext(entity, WriteOperation.Update);

        return await WriteCore(entity, context);
    }

    public virtual async Task<DeleteResult> Delete(long id, object extraData = null)
    {
        if (typeof(TEntity).IsAssignableTo(typeof(IAdminAsset)))
            await _permissionService.EnsureHasAssetFullAccess();

        if (typeof(TEntity).IsAssignableTo(typeof(IAdminCategory)))
            await _permissionService.EnsureHasCategoryFullAccess();

        using var scope = Log.BeginScope(this);

        Log.LogInformation("Entity ID={Id} deleting started", id);

        var entity = await ReadAlgorithm.GetForDeletion(id).SingleOrDefaultAsyncSafe();

        if (entity == null)
            return new DeleteResult {Ok = false, IsEntityFound = false};

        var context = CreateCallContext(entity, WriteOperation.Delete);
        await WriteCore(entity, context);

        return new DeleteResult {Ok = true, IsEntityFound = true};
    }

    public void Dispose()
    {
        WriteAlgorithm?.Dispose();
        UnitOfWork?.Dispose();
    }

    protected virtual async Task<TEntity> WriteCore(TEntity entity, CallContext context)
    {
        Log.LogTrace("Running input entity modification...");
        await WriteAlgorithm.ModifyInputEntity(entity, context);

        // No validation due deletion
        if (context.Operation != WriteOperation.Delete)
        {
            Log.LogTrace("Validating entity");
            var validationResult = await WriteAlgorithm.Validate(entity, context);

            if (!validationResult.IsValid)
            {
                Log.LogWarning("Validation failed, aborting {ContextOperation} operation", context.Operation);

                throw new EntityValidationException(validationResult, context.Operation);
            }
        }

        await using (var transaction = await UnitOfWork.BeginTransaction())
        {
            try
            {
                UnitOfWork.Track(entity);

                // CanSave is called in bounds of transaction to avoid
                // inconsistency if db would be modified between check and save
                var canSave = await WriteAlgorithm.CanSave(entity, context);
                if (!canSave)
                {
                    Log.LogWarning("Saving of {Name} prohibited", typeof(TEntity).Name);

                    throw new EntityWriteException($"{context.Operation} is not allowed", context.Operation, 403);
                }

                Log.LogTrace("Saving allowed");

                if (context.Operation == WriteOperation.Delete)
                {
                    Log.LogTrace("Mark entity for deletion");
                    await WriteAlgorithm.MarkForDeletion(entity, context);
                }
                else
                {
                    Log.LogTrace("Pre-saving...");
                    await WriteAlgorithm.PreSave(entity, context);
                }

                await UnitOfWork.SaveChanges();

                Log.LogTrace("Changes saved to database");
                Log.LogTrace("Running after save changes processing...");
                await WriteAlgorithm.AfterSaveChanges(entity, context);

                await transaction.Commit();
                Log.LogTrace("Transaction committed");
            }
            catch (Exception ex)
            {
                await transaction.Rollback();
                Log.LogError(ex, "{ContextOperation} of {Name} failed", context.Operation, typeof(TEntity).Name);

                throw new EntityWriteException(ex.Message, ex, context.Operation);
            }
        }

        try
        {
            Log.LogTrace("Running after commit...");

            await WriteAlgorithm.AfterCommit(entity, context);

            Log.LogTrace("After commit completed");
        }
        catch (EntityWriteException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "{ContextOperation} of {Name} failed", context.Operation, typeof(TEntity).Name);

            throw new EntityWriteException(ex.Message, ex, context.Operation);
        }

        if (context.Operation == WriteOperation.Delete)
            return entity;

        var result = await ReadAlgorithm.GetOne(entity.Id).SingleAsyncSafe();

        return result;
    }

    private CallContext CreateCallContext(TEntity entity, WriteOperation operation)
    {
        return new CallContext(entity, operation);
    }
}