using System;
using System.Threading.Tasks;
using Common.Models.Files;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;

#pragma warning disable CS1998

namespace AssetServer.ModelBinders;

internal sealed class ResolutionModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var resolutionName = bindingContext.HttpContext.GetRouteValue(bindingContext.FieldName) as string;
        if (string.IsNullOrEmpty(resolutionName))
            throw new ArgumentNullException(bindingContext.FieldName);

        if (ParseResolutionInputArg(resolutionName, out var resolution))
            bindingContext.Result = ModelBindingResult.Success(resolution);
        else
            throw new InvalidOperationException($"Can not parse resolution {resolutionName}");
    }

    private bool ParseResolutionInputArg(string input, out Resolution result)
    {
        return Enum.TryParse('_' + input, true, out result);
    }
}