using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.Infrastructure.Services;
using Common.Infrastructure.Utils;
using Common.Models.Database.Interfaces;

namespace Frever.AdminService.Core.Services.EntityServices;

public partial class DefaultEntityWriteAlgorithm<TEntity>
{
    protected void DeclareDependency<TDependency>(
        Expression<Func<TEntity, TDependency>> accessorExpr,
        IEntityWriteAlgorithm<TDependency> processor,
        bool deleteDependentEntityOnDeletion
    )
        where TDependency : class, IEntity
    {
        ArgumentNullException.ThrowIfNull(accessorExpr);
        ArgumentNullException.ThrowIfNull(processor);

        var accessor = accessorExpr.Compile();

        Func<TEntity, CallContext, Task<TReturn>> CreateProcessorWithResult<TReturn>(
            Func<IEntityWriteAlgorithm<TDependency>, Func<TDependency, CallContext, Task<TReturn>>> processorFn,
            TReturn defaultReturn
        )
        {
            var fn = processorFn(processor);

            return async (entity, context) =>
                   {
                       var dep = accessor(entity);

                       return dep == null ? defaultReturn : await fn(dep, context);
                   };
        }

        Func<TEntity, CallContext, Task> CreateProcessor(
            Func<IEntityWriteAlgorithm<TDependency>, Func<TDependency, CallContext, Task>> processorFn
        )
        {
            var fn = processorFn(processor);

            return async (entity, context) =>
                   {
                       var dep = accessor(entity);
                       if (dep != null)
                           await fn(dep, context);
                   };
        }

        _dependencies.Add(
            new Dependency
            {
                NavigationPropertyPath = accessorExpr.ToMemberName(),
                DeleteDependentEntities = deleteDependentEntityOnDeletion,
                ModifyInputEntity = CreateProcessor(a => a.ModifyInputEntity),
                Validate = CreateProcessorWithResult(a => a.Validate, ValidationResult.Valid),
                CanSave = CreateProcessorWithResult(a => a.CanSave, true),
                PreSave = CreateProcessor(a => a.PreSave),
                AfterSaveChanges = CreateProcessor(a => a.AfterSaveChanges),
                AfterCommit = CreateProcessor(a => a.AfterCommit),
                MarkForDeletion = CreateProcessor(a => a.MarkForDeletion),
                CancelEntityDeletion = CreateProcessor(a => a.CancelEntityDeletion)
            }
        );
    }

    protected void DeclareDependency<TDependency>(
        Expression<Func<TEntity, IEnumerable<TDependency>>> accessorExpr,
        IEntityWriteAlgorithm<TDependency> processor,
        bool deleteDependentEntityOnDeletion
    )
        where TDependency : class, IEntity
    {
        ArgumentNullException.ThrowIfNull(accessorExpr);
        ArgumentNullException.ThrowIfNull(processor);

        var accessor = accessorExpr.Compile();

        Func<TEntity, CallContext, Task> CreateProcessor(
            Func<IEntityWriteAlgorithm<TDependency>, Func<TDependency, CallContext, Task>> processorFn
        )
        {
            var fn = processorFn(processor);

            return async (entity, context) =>
                   {
                       var dep = accessor(entity);
                       if (dep != null)
                           await dep.Select(i => fn(i, context)).Collapse();
                   };
        }

        Func<TEntity, CallContext, Task<TResult>> CreateProcessorWithResult<TResult>(
            Func<IEntityWriteAlgorithm<TDependency>, Func<TDependency, CallContext, Task<TResult>>> processorFn,
            Func<TResult, TResult, TResult> reduce,
            TResult defaultResult
        )
        {
            var fn = processorFn(processor);

            return async (entity, context) =>
                   {
                       var dep = accessor(entity);

                       return dep != null ? await dep.Select(i => fn(i, context)).Collapse(reduce, defaultResult) : defaultResult;
                   };
        }

        _dependencies.Add(
            new Dependency
            {
                NavigationPropertyPath = accessorExpr.ToMemberName(),
                DeleteDependentEntities = deleteDependentEntityOnDeletion,
                ModifyInputEntity = CreateProcessor(a => a.ModifyInputEntity),
                Validate = CreateProcessorWithResult(a => a.Validate, (acc, r) => acc.Compose(r), ValidationResult.Valid),
                CanSave = CreateProcessorWithResult(a => a.CanSave, (acc, r) => acc && r, true),
                PreSave = CreateProcessor(a => a.PreSave),
                AfterSaveChanges = CreateProcessor(a => a.AfterSaveChanges),
                AfterCommit = CreateProcessor(a => a.AfterCommit),
                MarkForDeletion = CreateProcessor(a => a.MarkForDeletion),
                CancelEntityDeletion = CreateProcessor(a => a.CancelEntityDeletion)
            }
        );
    }

    private class Dependency
    {
        public bool DeleteDependentEntities { get; set; }

        public string NavigationPropertyPath { get; set; }

        public Func<TEntity, CallContext, Task> ModifyInputEntity { get; set; }

        public Func<TEntity, CallContext, Task<ValidationResult>> Validate { get; set; }

        public Func<TEntity, CallContext, Task<bool>> CanSave { get; set; }

        public Func<TEntity, CallContext, Task> PreSave { get; set; }

        public Func<TEntity, CallContext, Task> AfterSaveChanges { get; set; }

        public Func<TEntity, CallContext, Task> AfterCommit { get; set; }

        public Func<TEntity, CallContext, Task> MarkForDeletion { get; set; }

        public Func<TEntity, CallContext, Task> CancelEntityDeletion { get; set; }
    }
}