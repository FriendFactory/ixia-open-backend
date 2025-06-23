using System;
using System.Reflection;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Newtonsoft.Json.Linq;

namespace Frever.AdminService.Core.JsonToModel;

public static class EntityPartialUpdateServiceExtensions
{
    private static readonly MethodInfo PartialUpdateMethod =
        typeof(EntityPartialUpdateService).GetMethod(nameof(EntityPartialUpdateService.UpdateEntityAsync));

    public static async Task<object> UpdateEntityAsync(
        this IEntityPartialUpdateService service,
        JObject input,
        long? id,
        Type entityType,
        bool modifyDbContext
    )
    {
        return await PartialUpdateMethod.InvokeGenericAsync<object>(
                   entityType,
                   service,
                   input,
                   id,
                   modifyDbContext
               );
    }
}