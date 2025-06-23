using System;
using System.Reflection;
using System.Threading.Tasks;
using Common.Models.Database.Interfaces;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

#pragma warning disable CS1998

namespace AssetServer.ModelBinders;

public class EntityModelType(Type entityType)
{
    public Type EntityType { get; } = entityType ?? throw new ArgumentNullException(nameof(entityType));
}

internal class EntityModelTypeBinder : IModelBinder
{
    private static readonly string EntitiesNamespace = typeof(Song).Namespace;
    private static readonly Assembly EntitiesAssembly = typeof(Song).Assembly;

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var assetTypeName = bindingContext.HttpContext.GetRouteValue(bindingContext.FieldName) as string;
        if (string.IsNullOrEmpty(assetTypeName))
            throw new ArgumentNullException();

        var type = EntitiesAssembly.GetType($"{EntitiesNamespace}.{assetTypeName}");
        if (type == null || !typeof(IFileOwner).IsAssignableFrom(type))
            throw new Exception($"Unexpected asset type: {assetTypeName}");

        bindingContext.Result = ModelBindingResult.Success(new EntityModelType(type));
    }
}

public class EntityModelTypeBinderProvider : IModelBinderProvider
{
    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        return context.Metadata.ModelType == typeof(EntityModelType) ? new EntityModelTypeBinder() : null;
    }
}