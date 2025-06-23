using System;
using System.Threading.Tasks;
using Common.Models.Database.Interfaces;

namespace Frever.AdminService.Core.Services.EntityServices;

/// <summary>
///     Provides a set of operations on entity called by <see cref="DefaultWriteEntityService{TEntity}" />.
///     Operations on algorithm could be safely composed.
/// </summary>
public interface IEntityWriteAlgorithm<in TEntity> : IDisposable
    where TEntity : class, IEntity
{
    /// <summary>
    ///     The most first step before even applying to db context.
    ///     Allows to modify an entity before tracking by db context,
    ///     for example for removing some references to prevent them from being captured by db context.
    ///     Runs outside of transaction.
    /// </summary>
    Task ModifyInputEntity(TEntity entity, CallContext context);

    /// <summary>
    ///     Checks if provided entity is valid.
    ///     Executed before applying entity to db context.
    /// </summary>
    Task<ValidationResult> Validate(TEntity entity, CallContext context);

    /// <summary>
    ///     Checks if current user permitted to save an entity.
    ///     Called right after validation on valid entity only.
    ///     Method should return false if entity not allowed to be written.
    ///     It's ok to also throw exception inside this method to provide more informative
    ///     error description.
    ///     Method is called in transaction bounds to avoid inconsistency if data would be modified
    ///     between check and actual saving.
    /// </summary>
    Task<bool> CanSave(TEntity entity, CallContext context);

    /// <summary>
    ///     Called for create and update operations (but not delete).
    ///     Called on entity added to context before calling SaveChanges on context.
    ///     The method is called in the bounds of open transaction, so it's safe to call db here.
    /// </summary>
    Task PreSave(TEntity entity, CallContext context);

    /// <summary>
    ///     Execute some actions after saving to database but in transaction scope.
    ///     Any error or exception would cause transaction to be rolled back.
    /// </summary>
    Task AfterSaveChanges(TEntity entity, CallContext context);

    /// <summary>
    ///     Called after data have successfully written to database, so all entities got their IDs etc.
    /// </summary>
    Task AfterCommit(TEntity entity, CallContext context);

    /// <summary>
    ///     Called after PreProcess and before PostProcess if entity needs to be deleted
    ///     (usually if user requested delete operation).
    ///     Call it on dependent entity to delete it from database and perform required cleanup actions.
    /// </summary>
    Task MarkForDeletion(TEntity entity, CallContext context);

    /// <summary>
    ///     Marks deleted entity as modified.
    /// </summary>
    Task CancelEntityDeletion(TEntity entity, CallContext context);
}