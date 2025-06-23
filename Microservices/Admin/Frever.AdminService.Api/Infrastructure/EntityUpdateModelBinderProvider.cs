using System;
using Common.Models.Database.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Frever.AdminService.Api.Infrastructure;

public class EntityUpdateModelBinderProvider : IModelBinderProvider
{
    public IModelBinder GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        using var scope = context.Services.CreateScope();

        return typeof(IEntity).IsAssignableFrom(context.Metadata.ModelType) ? new EntityUpdateModelBinder() : null;
    }
}