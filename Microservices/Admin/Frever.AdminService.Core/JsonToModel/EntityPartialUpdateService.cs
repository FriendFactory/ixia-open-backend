using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.Infrastructure.Database;
using Common.Infrastructure.Utils;
using Common.Models.Database.Interfaces;
using Frever.AdminService.Core.Utils;
using Frever.Shared.MainDb;
using Frever.Shared.MainDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json.Linq;

namespace Frever.AdminService.Core.JsonToModel;

public class EntityPartialUpdateService(WriteDbContext dbContext) : IEntityPartialUpdateService
{
    private readonly WriteDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<TEntity> UpdateEntityAsync<TEntity>(JObject input, long? id = null, bool modifyDbContext = false)
        where TEntity : class, new()
    {
        var entityType = _dbContext.Model.GetEntityTypes().FirstOrDefault(t => t.ClrType == typeof(TEntity)) ??
                         throw new ArgumentException($"Entity metadata is not found for type {typeof(TEntity).Name}");

        var updatedEntity = await UpdateEntityAsyncCore(input, id, entityType, modifyDbContext);

        return (TEntity) updatedEntity;
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    private async Task<object> UpdateEntityAsyncCore(JObject input, long? id, IEntityType entityType, bool modifyDbContext)
    {
        var entityId = GetEntityId(input, id, entityType);
        object entityToUpdate;

        switch (entityId)
        {
            case long eid:
                var inputProps = input.Properties().Select(p => p.Name).ToArray();
                entityToUpdate = await GetType()
                                      .GetMethod(nameof(GetEntityWithAggregates))
                                      .InvokeGenericAsync<object>(
                                           entityType.ClrType,
                                           this,
                                           eid,
                                           entityType,
                                           inputProps,
                                           modifyDbContext,
                                           input
                                       );

                if (eid < 0 && modifyDbContext)
                    await GetType()
                         .GetMethod(nameof(MarkEntityAsDeleted))
                         .InvokeGenericAsync<object>(entityType.ClrType, this, eid, modifyDbContext);

                break;

            default:
                entityToUpdate = Activator.CreateInstance(entityType.ClrType);
                if (modifyDbContext)
                    await _dbContext.AddAsync(entityToUpdate);

                break;
        }

        var restrictedProperties = new List<string> {nameof(IGroupAccessible.GroupId), nameof(IUploadedByUser.UploaderUserId)};

        var properties = entityType.GetProperties().Where(prop => restrictedProperties.All(p => p != prop.Name)).ToArray();

        var restrictedTypes = new List<Type> {typeof(Group), typeof(User)};

        var navigations = entityType.GetNavigations().Where(p => restrictedTypes.All(t => t != p.ClrType)).ToArray();

        foreach (var prop in input.Properties())
        {
            var entityProperty = properties.FirstOrDefault(p => p.Name.ToCamelCase().Equals(prop.Name, StringComparison.OrdinalIgnoreCase));

            if (entityProperty != null)
            {
                var propValue = input[prop.Name].ToObject(entityProperty.ClrType);
                entityProperty.PropertyInfo.GetSetMethod().Invoke(entityToUpdate, [propValue]);

                if (entityProperty.IsForeignKey())
                {
                    var fk = entityProperty.GetContainingForeignKeys().FirstOrDefault();
                    if (fk != null)
                    {
                        var fkProp = fk.DependentToPrincipal;
                        var fkJProp = input.Property(fkProp.Name.ToCamelCase());

                        if (fkJProp != null)
                            if (propValue != null && !EqualityComparer<long>.Default.Equals(0, (long) propValue))
                            {
                                var inputFkValue = input[fkJProp.Name].ToObject(fkProp.ClrType);
                                if (inputFkValue == null)
                                {
                                    fkProp.PropertyInfo.SetValue(entityToUpdate, null);
                                }
                                else
                                {
                                    var fkEntityPrimaryKey = GetEntityId((JObject) input[fkJProp.Name], null, fk.PrincipalEntityType);

                                    if (Math.Abs(fkEntityPrimaryKey ?? 0) != Math.Abs((long) propValue))
                                        throw new InvalidOperationException(
                                            $"Entity {entityType.Name} has both" +
                                            $" {entityProperty.Name} and {fkProp.Name} set to non-default values"
                                        );
                                }
                            }
                    }
                }

                continue;
            }

            var nav = navigations.FirstOrDefault(n => n.Name.ToCamelCase().Equals(prop.Name, StringComparison.OrdinalIgnoreCase));

            if (nav != null)
            {
                var type = nav.ClrType;

                if (type.IsCollection())
                {
                    var collection = nav.GetGetter().GetClrValue(entityToUpdate);
                    var itemEntityType = nav.TargetEntityType;

                    if (itemEntityType == null)
                        throw new InvalidOperationException($"Entity type not found for property {entityType.Name}.{nav.Name}");

                    var collectionJson = (JArray) input[prop.Name].ToObject(typeof(JArray));

                    if (collectionJson == null)
                        continue;

                    await GetType()
                         .GetMethod(nameof(UpdateRelatedEntitiesCollection))
                         .InvokeGenericAsync<object>(
                              itemEntityType.ClrType,
                              this,
                              collection,
                              collectionJson,
                              itemEntityType,
                              modifyDbContext
                          );
                }
                else
                {
                    var entityFromJson = (JObject) input[prop.Name].ToObject(typeof(JObject));

                    if (entityFromJson == null)
                    {
                        nav.PropertyInfo.SetValue(entityToUpdate, null);
                    }
                    else
                    {
                        var relatedEntity = await UpdateEntityAsyncCore(entityFromJson, null, nav.TargetEntityType, modifyDbContext);

                        nav.PropertyInfo.GetSetMethod().Invoke(entityToUpdate, [relatedEntity]);
                    }
                }
            }
            else
            {
                var customProp = entityToUpdate.GetType()
                                               .GetProperties()
                                               .Where(pi => restrictedProperties.All(p => p != pi.Name))
                                               .Where(pi => restrictedTypes.All(t => t != pi.PropertyType))
                                               .FirstOrDefault(x => x.Name.ToCamelCase() == prop.Name);

                if (customProp != null && customProp.CanWrite)
                {
                    var value = input[prop.Name].ToObject(customProp.PropertyType);

                    customProp.SetValue(entityToUpdate, value);
                }
            }
        }

        return entityToUpdate;
    }

    private static long? GetEntityId(JObject input, long? id, IEntityType entityType)
    {
        if (id != null)
            return id.Value;

        var propName = nameof(IEntity.Id).ToCamelCase();

        if (!input.ContainsKey(propName))
            return null;

        var innerId = (long) input[propName].ToObject(typeof(long));

        if (innerId == 0)
            return null;

        return innerId;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public async Task<TEntity> MarkEntityAsDeleted<TEntity>(long id, bool modifyDbContext)
        where TEntity : class
    {
        if (id < 0)
        {
            var entity = await _dbContext.Set<TEntity>().FindAsync(-id);
            if (entity != null)
            {
                if (modifyDbContext)
                    _dbContext.Set<TEntity>().Remove(entity);

                return entity;
            }
        }

        return null;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public async Task<TEntity> GetEntityWithAggregates<TEntity>(
        long id,
        IEntityType entityType,
        string[] inputProps,
        bool modifyDbContext,
        JObject input
    )
        where TEntity : class
    {
        var idParam = Expression.Parameter(typeof(TEntity), "id");
        var filter = Expression.Lambda<Func<TEntity, bool>>(
            Expression.Equal(Expression.Property(idParam, nameof(IEntity.Id)), Expression.Constant(id > 0 ? id : -id)),
            idParam
        );
        var query = _dbContext.Set<TEntity>().Where(filter);

        if (!modifyDbContext)
            query = query.AsNoTracking();

        foreach (var nav in entityType.GetNavigations())
        {
            var prop = nav.Name;

            if (nav.IsManyToMany() && !nav.IsCollection)
            {
                query = query.Include(prop);
                if (!modifyDbContext)
                    query = query.AsNoTracking();
            }
            else if (inputProps.Any(p => StringComparer.OrdinalIgnoreCase.Equals(prop.ToCamelCase(), p)))
            {
                var inputValue = input[prop.ToCamelCase()];
                if (inputValue is JArray array && array.Count == 0)
                    continue;

                query = query.Include(prop);

                if (!modifyDbContext)
                    query = query.AsNoTracking();
            }
        }

        var result = await query.FirstOrDefaultAsyncSafe();

        if (result == null)
            throw new InvalidOperationException($"Entity {typeof(TEntity).Name} with ID = {id} is not found");

        if (id < 0)
        {
            var idProp = typeof(TEntity).GetProperty(nameof(IEntity.Id));
            idProp?.SetValue(result, id);
        }

        return result;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public async Task UpdateRelatedEntitiesCollection<TEntity>(
        ICollection<TEntity> collection,
        JArray values,
        IEntityType itemEntityType,
        bool modifyDbContext
    )
        where TEntity : class
    {
        var originalContent = collection.ToArray();

        collection.Clear();

        foreach (JObject value in values)
        {
            var entity = (TEntity) await UpdateEntityAsyncCore(value, null, itemEntityType, modifyDbContext);

            collection.Add(entity);

            var idProperty = typeof(TEntity).GetProperty("Id");

            if (idProperty == null)
                continue;

            if ((long) idProperty.GetValue(entity) == 0)
            {
                if (modifyDbContext)
                    await _dbContext.AddAsync(entity);
            }
            else
            {
                var entityParam = Expression.Parameter(typeof(TEntity), "entity");
                var filter = Expression.Lambda<Func<TEntity, bool>>(
                                            Expression.NotEqual(
                                                Expression.Property(entityParam, nameof(IEntity.Id)),
                                                Expression.Property(Expression.Constant(entity), nameof(IEntity.Id))
                                            ),
                                            entityParam
                                        )
                                       .Compile();

                originalContent = originalContent.Where(filter).ToArray();
            }
        }

        if (modifyDbContext)
            foreach (var entityToDelete in originalContent)
                _dbContext.Set<TEntity>().Remove(entityToDelete);
    }
}