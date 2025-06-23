using System;
using System.IO;
using System.Threading.Tasks;
using Frever.AdminService.Core.JsonToModel;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Infrastructure;

public class EntityUpdateModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var idStr = bindingContext.HttpContext.GetRouteValue("id");
        long? id = null;

        if (idStr != null && long.TryParse(idStr.ToString(), out var idLong))
            id = idLong;

        var json = await ReadModelToBindAsync(bindingContext);

        var input = JObject.Parse(json);

        var entityUpdateService = bindingContext.HttpContext.RequestServices.GetRequiredService<IEntityPartialUpdateService>();

        var entity = await entityUpdateService.UpdateEntityAsync(input, id, bindingContext.ModelType, false);

        bindingContext.Result = ModelBindingResult.Success(entity);
    }

    private static async Task<string> ReadModelToBindAsync(ModelBindingContext context)
    {
        if (context.HttpContext.Request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            var content = context.HttpContext.Request.Form[""][0];

            return content;
        }

        using var sr = new StreamReader(context.HttpContext.Request.Body);

        return await sr.ReadToEndAsync();
    }
}