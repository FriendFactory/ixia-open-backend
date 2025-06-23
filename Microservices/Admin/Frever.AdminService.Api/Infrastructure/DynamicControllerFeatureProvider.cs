using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common.Models.Database.Interfaces;
using Frever.AdminService.Api.Controllers;
using Frever.AdminService.Core;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Frever.AdminService.Api.Infrastructure;

public class DynamicControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        var currentAssembly = typeof(DynamicControllerFeatureProvider).Assembly;

        var entityTypes = ServicesCollectionExtensions.GetAllEntityTypes<IEntity, Song>().ToArray();
        var existingControllers = currentAssembly.GetExportedTypes()
                                                 .Where(
                                                      t => (t.BaseType is {IsGenericType: true} &&
                                                            t.BaseType.GetGenericTypeDefinition() == typeof(EntityControllerBase<>)) ||
                                                           (typeof(ControllerBase).IsAssignableFrom(t) &&
                                                            t.IsDefined(typeof(ReplaceEntityControllerAttribute)))
                                                  )
                                                 .Where(t => t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
                                                 .ToArray();

        var entitiesWithoutController = entityTypes.Where(
                                                        entityType =>
                                                        {
                                                            return !existingControllers.Any(
                                                                       controllerType
                                                                           => typeof(EntityControllerBase<>).MakeGenericType(entityType)
                                                                                 .IsAssignableFrom(controllerType) ||
                                                                              controllerType
                                                                                 .GetCustomAttribute<ReplaceEntityControllerAttribute>()
                                                                                ?.EntityType == entityType
                                                                   ) && !existingControllers.Any(
                                                                       controllerType
                                                                           => controllerType.Name.Equals(
                                                                                  $"{entityType.Name}Controller",
                                                                                  StringComparison.OrdinalIgnoreCase
                                                                              ) ||
                                                                              controllerType.Name.Equals(
                                                                                  $"{entityType.Name}sController",
                                                                                  StringComparison.OrdinalIgnoreCase
                                                                              )
                                                                   );
                                                        }
                                                    )
                                                   .ToArray();

        foreach (var entityType in entitiesWithoutController)
        {
            feature.Controllers.Add(typeof(EntityControllerBase<>).MakeGenericType(entityType).GetTypeInfo());
        }
    }
}