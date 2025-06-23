using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Protobuf;
using Common.Models.Database.Interfaces;
using Frever.AdminService.Api.Controllers.Utils;
using Frever.AdminService.Api.Infrastructure.OData;
using Frever.AdminService.Core.Services.EntityServices;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EntityControllerBase<T>(IWriteEntityService<T> writeEntityService) : ControllerBase
    where T : class, IEntity
{
    private readonly IWriteEntityService<T> _writeEntityService =
        writeEntityService ?? throw new ArgumentNullException(nameof(writeEntityService));

    [HttpGet]
    public virtual async Task<ActionResult> GetAll(ODataQueryOptions<T> options)
    {
        if (Request.IsProtobufAccepted())
        {
            using var scope = HttpContext.RequestServices.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<IReadEntityService<T>>();
            object result =
                (await service.GetAll(q => q.ApplyExpand(options), new GetAllParams {Expand = options?.SelectExpand?.RawExpand}))
               .ApplyODataRequest(options)
               .Cast<T>()
               .ToArray();

            return Ok(result);
        }
        else
        {
            using var scope = HttpContext.RequestServices.CreateScope();

            var service = scope.ServiceProvider.GetRequiredService<IReadEntityService<T>>();
            object result =
                (await service.GetAll(q => q.ApplyExpand(options), new GetAllParams {Expand = options?.SelectExpand?.RawExpand}))
               .ApplyODataRequest(options)
               .ApplyODataSelect(
                    options,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    }
                )
               .Cast<object>()
               .ToArray();

            return Ok(result);
        }
    }

    [HttpGet("{id}")]
    public virtual async Task<ActionResult> GetById(long id)
    {
        using var scope = HttpContext.RequestServices.CreateScope();

        var service = scope.ServiceProvider.GetRequiredService<IReadEntityService<T>>();
        var model = await service.GetOne(id);

        if (model == null)
            return NotFound();

        return Ok(model);
    }

    [HttpPost]
    public virtual async Task<ActionResult<T>> Post([FromBody] T model)
    {
        try
        {
            var result = await _writeEntityService.Create(model).ConfigureAwait(false);

            return Ok(result);
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(ex.ValidationErrors);
        }
    }

    [HttpPatch]
    public virtual async Task<ActionResult<T>> Update([JsonBinder] T model)
    {
        try
        {
            var result = await _writeEntityService.Update(model).ConfigureAwait(false);

            return Ok(result);
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(ex.ValidationErrors);
        }
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(long id)
    {
        using (var scope = HttpContext.RequestServices.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IReadEntityService<T>>();
            var entity = await service.GetOne(id);

            if (entity == null)
                return NotFound();

            await _writeEntityService.Delete(entity.Id).ConfigureAwait(false);
        }

        return NoContent();
    }
}