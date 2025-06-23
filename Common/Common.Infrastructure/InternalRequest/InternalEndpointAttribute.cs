using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Common.Infrastructure.InternalRequest;

public class InternalEndpointAttribute : ActionFilterAttribute
{
    public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var isInternalRequest = context.HttpContext.IsInternalRequest();

        if (isInternalRequest)
            return base.OnActionExecutionAsync(context, next);

        context.Result = new StatusCodeResult((int) HttpStatusCode.Forbidden);
        return Task.CompletedTask;
    }
}