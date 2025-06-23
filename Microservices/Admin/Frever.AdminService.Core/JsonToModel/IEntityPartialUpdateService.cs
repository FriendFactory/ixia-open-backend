using System;
using System.Threading.Tasks;
using Common.Models.Database.Interfaces;
using Newtonsoft.Json.Linq;

namespace Frever.AdminService.Core.JsonToModel;

public interface IEntityPartialUpdateService : IDisposable
{
    /// <summary>
    ///     Performs partial update of entity and relations from <paramref name="input" /> using following rules:
    ///     - Method updates properties of the entity which exist in <paramref name="input" /> JSON;
    ///     - Method also tries to partial update navigation properties if they exists in JSON;
    ///     - Entity to update could be retrieved from context or created new and then added to context:
    ///     -- If <see cref="IEntity.Id" /> provided in JSON or <paramref name="id" /> is not null then entity retrieved from
    ///     context
    ///     -- If no ID provided then entity is created and added to context
    ///     -- The same rules applied to related entities.
    /// </summary>
    /// <param name="input">The JSON contains updates to apply.</param>
    /// <param name="id">Optional identifier of root entity.</param>
    /// <param name="modifyDbContext">
    ///     If set to true updated entities will be tracked in DbContext.
    ///     Otherwise will be loaded as no tracking.
    ///     Default is false.
    /// </param>
    /// <typeparam name="TEntity">Type of entity root.</typeparam>
    /// <returns>Updated entity.</returns>
    Task<TEntity> UpdateEntityAsync<TEntity>(JObject input, long? id = null, bool modifyDbContext = false)
        where TEntity : class, new();
}