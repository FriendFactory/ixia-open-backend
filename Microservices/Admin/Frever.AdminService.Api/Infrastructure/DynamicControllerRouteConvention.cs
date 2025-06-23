using System;
using Frever.AdminService.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Frever.AdminService.Api.Infrastructure;

public class DynamicControllerRouteConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        ArgumentNullException.ThrowIfNull(controller);

        if (controller.ControllerType.IsGenericType &&
            controller.ControllerType.GetGenericTypeDefinition() == typeof(EntityControllerBase<>))
        {
            var genericType = controller.ControllerType.GenericTypeArguments[0];
            var path = $"api/{genericType.Name}";
            var routeAttribute = new RouteAttribute(path);

            // TODO: Remove this line and also [Route] attribute
            // on EntityControllerBase after updating to netcoreapp3.0
            // This is a temp fix for get Swagger working
            controller.Selectors.Clear();
            controller.Selectors.Add(new SelectorModel {AttributeRouteModel = new AttributeRouteModel(routeAttribute)});
        }
    }
}