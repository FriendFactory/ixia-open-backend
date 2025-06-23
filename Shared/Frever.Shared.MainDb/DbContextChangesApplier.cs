using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Common.Models.Database.Interfaces;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#pragma warning disable CS0618

namespace Frever.Shared.MainDb;

/// <summary>
///     WARNING: This class is not intended to work with composite primary keys
///     except keys for many-to-many relations.
/// </summary>
public class DbContextChangesApplier<TDbContext>(TDbContext dbContext) : IDisposable, IAsyncDisposable
    where TDbContext : DbContext
{
    public TDbContext DbContext { get; } = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async ValueTask DisposeAsync()
    {
        if (DbContext != null)
            await DbContext.DisposeAsync();
    }

    public void Dispose()
    {
        DbContext?.Dispose();
    }

    public void ApplyEntityToDbContext<TEntity>(TEntity entity)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(entity);

        ApplyEntityToDbContextCore(entity, null, null, null);
    }

    private EntityOperation ApplyEntityToDbContextCore<T>(
        T entity,
        IEntityType parentEntityType,
        object parentEntity,
        INavigation navigation
    )
        where T : class
    {
        if (entity == null)
            return EntityOperation.Keep;

        var entityType = DbContext.Model.FindEntityType(entity.GetType());

        if (entityType == null)
            return EntityOperation.Keep;

        var entityInContext = IsEntityInContext(entity);

        if (entityInContext)
            return EntityOperation.Keep;

        var (state, operation) = GetEntityState(
            entity,
            entityType,
            parentEntityType,
            parentEntity,
            navigation
        );

        if (state == EntityState.Deleted || operation == EntityOperation.DeleteFromParentCollection)
            FixKeysForDeletedEntities(entity, entityType);

        if (parentEntity != null && entity.GetType() == typeof(Group))
        {
            DbContext.Entry(entity).State = EntityState.Unchanged;
            return operation;
        }

        DbContext.Entry(entity).State = state;

        var navs = entityType.GetNavigations().ToArray();

        foreach (var n in navs)
        {
            if (n.IsManyToMany() && !n.IsCollection)
            {
                GetType()
                   .GetMethod(nameof(ApplyOne2OneChanges))
                  ?.MakeGenericMethod(n.TargetEntityType.ClrType)
                   .Invoke(this, [n, entity]);

                continue;
            }

            var nav = n.GetGetter().GetClrValue(entity);

            switch (nav)
            {
                case null:
                    break;
                case IEnumerable collection:
                {
                    var itemsToDelete = new List<object>();
                    foreach (var item in collection)
                    {
                        var currentState = ApplyEntityToDbContextCore(item, entityType, entity, n);

                        if (currentState == EntityOperation.DeleteFromParentCollection)
                            itemsToDelete.Add(item);
                    }

                    var removeMethod = collection.GetType().GetMethod("Remove");
                    if (itemsToDelete.Count > 0 && removeMethod != null)
                        foreach (var item in itemsToDelete)
                            removeMethod.Invoke(collection, [item]);

                    break;
                }
                case object value:
                {
                    ApplyEntityToDbContextCore(value, entityType, entity, n);

                    break;
                }
            }
        }

        return operation;
    }


    private (EntityState, EntityOperation) GetEntityState(
        object entity,
        IEntityType entityType,
        IEntityType parentEntityType,
        object parent,
        INavigation nav = null
    )
    {
        var pk = entityType.GetKeys().Where(k => k.IsPrimaryKey());
        var props = pk.SelectMany(k => k.Properties).ToArray();

        // NOTE: If there are more than 1 key, that's probably many-to-many relation
        // Such relations should never be modified, just added or deleted
        if (props.Length > 1 && parentEntityType != null && parent != null)
        {
            var idPropName = nameof(IEntity.Id);
            var parentIdProp = parentEntityType.GetProperties().First(x => x.Name == idPropName);

            var keysInfo = nav.GetManyToManyKeyInfo();

            var childKeyProperty = keysInfo.OtherSideProperty;
            var parentKeyProperty = keysInfo.MainSideProperty;

            var childKeyValue = childKeyProperty.GetGetter().GetClrValue(entity);
            var parentKeyValue = parentKeyProperty.GetGetter().GetClrValue(entity);

            var childEntityState = GetSingleKeyState(childKeyValue);
            var parentKeyEntityState = GetSingleKeyState(parentKeyValue);

            var parentId = parentIdProp.GetGetter().GetClrValue(parent);
            var parentEntityState = GetSingleKeyState(parentId);

            //we add many-to-many if parentKey = 0
            //we delete many-to-many if childKey = negative value
            //we keep it as not modified if parentKey and childKey > 0

            switch (parentEntityState)
            {
                case EntityState.Added:
                    return (EntityState.Added, EntityOperation.Keep);
                case EntityState.Deleted:
                    return (EntityState.Unchanged, EntityOperation.DeleteFromParentCollection);
                default:
                    switch (childEntityState)
                    {
                        case EntityState.Modified:
                            if (nav.IsCollection)
                                return parentKeyEntityState == EntityState.Added
                                           ? (EntityState.Added, EntityOperation.Keep)
                                           : (EntityState.Unchanged, EntityOperation.Keep);
                            return (EntityState.Unchanged, EntityOperation.Keep);

                        case EntityState.Deleted:
                            return (EntityState.Unchanged, EntityOperation.DeleteFromParentCollection);
                        default:
                            return (childEntityState, EntityOperation.Keep);
                    }
            }
        }

        var value = props.First().GetGetter().GetClrValue(entity);

        return (GetSingleKeyState(value), EntityOperation.Keep);
    }


    public void ApplyOne2OneChanges<TChildEntity>(INavigation navigation, object entity)
        where TChildEntity : class
    {
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(entity);

        var parentEntity = entity as IEntity;
        if (parentEntity == null)
            throw new InvalidOperationException($"{entity.GetType().Name} is not an {nameof(IEntity)}");

        var childEntity = navigation.GetGetter().GetClrValue(entity);

        var keysInfo = navigation.GetManyToManyKeyInfo();
        if (keysInfo == null)
            throw new InvalidOperationException(
                $"Navigation {navigation.DeclaringEntityType.ClrType.Name}.{navigation.Name} " +
                $"is not a one-to-one relation with intermediate table"
            );

        var entityId = parentEntity.Id;

        var param = Expression.Parameter(typeof(TChildEntity), "e");
        var filter = Expression.Lambda<Func<TChildEntity, bool>>(
            Expression.Equal(Expression.Property(param, keysInfo.MainSideProperty.PropertyInfo), Expression.Constant(entityId)),
            param
        );

        var originalChildren = DbContext.Set<TChildEntity>().AsNoTracking().Where(filter).ToArray();

        foreach (var child in originalChildren)
            DbContext.Entry(child).State = EntityState.Deleted;

        if (childEntity != null)
        {
            DbContext.Entry(childEntity).State = EntityState.Added;
            navigation.PropertyInfo.SetValue(entity, childEntity);
        }
    }

    private void FixKeysForDeletedEntities(object entity, IEntityType entityType)
    {
        var pk = entityType.GetKeys().Where(k => k.IsPrimaryKey());
        var props = pk.SelectMany(k => k.Properties);

        foreach (var property in props)
        {
            var value = property.GetGetter().GetClrValue(entity);

            switch (value)
            {
                case long key when key < 0:
                    property.PropertyInfo.GetSetMethod().Invoke(entity, new object[] {-key});

                    break;
            }
        }
    }

    private EntityState GetSingleKeyState(object keyValue)
    {
        switch (keyValue)
        {
            case long id:
            {
                return id switch
                       {
                           0   => EntityState.Added,
                           < 0 => EntityState.Deleted,
                           _   => EntityState.Modified
                       };
            }
            case string key:
            {
                return string.IsNullOrWhiteSpace(key) ? EntityState.Added : EntityState.Modified;
            }
        }

        return EntityState.Modified;
    }

    private bool IsEntityInContext(object entity)
    {
        return (bool) GetType().GetMethod(nameof(IsEntityInContextCore)).MakeGenericMethod(entity.GetType()).Invoke(this, new[] {entity});
    }

    public bool IsEntityInContextCore<TEntity>(TEntity entity)
        where TEntity : class
    {
        var dbSet = DbContext.Set<TEntity>();

        if (dbSet == null)
            return false;

        if (DbContext.Entry(entity).State != EntityState.Detached)
            return true;

        if (dbSet.Local.Any(e => e == entity))
            return true;

        var entityType = DbContext.Model.FindEntityType(typeof(TEntity));
        var key = entityType?.FindPrimaryKey();
        var keyProp = key?.Properties.FirstOrDefault();

        if (keyProp == null)
            return false;

        var keyValue = (long) keyProp.GetGetter().GetClrValue(entity);

        if (keyValue == 0)
            return false;

        return DbContext.Set<TEntity>()
                        .Local.Any(
                             e => EqualityComparer<long>.Default.Equals(
                                 (long) keyProp.GetGetter().GetClrValue(e),
                                 keyValue > 0 ? keyValue : -keyValue
                             )
                         );
    }

    private enum EntityOperation
    {
        Keep,
        DeleteFromParentCollection
    }
}