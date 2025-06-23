using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Frever.AdminService.Api.Infrastructure;
using Frever.AdminService.Api.Infrastructure.OData;
using Frever.AdminService.Core.Services.EntityServices;
using Frever.AdminService.Core.Services.ReadinessService;
using Frever.Shared.MainDb.Entities;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#pragma warning disable CA2007

namespace Frever.AdminService.Api.Controllers;

[Route("api/Readiness")]
[ApiController]
[ReplaceEntityController(typeof(Readiness))]
public class ReadinessController(IReadEntityService<Readiness> readEntityService, IReadinessService readinessService) : ControllerBase
{
    private readonly IReadEntityService<Readiness> _readEntityService = readEntityService ?? throw new ArgumentNullException(nameof(readEntityService));
    private readonly IReadinessService _readinessService = readinessService ?? throw new ArgumentNullException(nameof(readinessService));

    [HttpGet]
    [Route("")]
    public async Task<IActionResult> GetAll(ODataQueryOptions<Readiness> options)
    {
        var all = await _readEntityService.GetAll(
                      q => q.ApplyExpand(options),
                      new GetAllParams {Expand = options?.SelectExpand?.RawExpand}
                  );

        var result = all.ApplyODataRequest(options)
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

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _readEntityService.GetOne(id);

        return Ok(result);
    }

    [HttpPost]
    [Route("")]
    public async Task<IActionResult> Create([FromBody] ReadinessInfo readiness)
    {
        try
        {
            var created = await _readinessService.Create(readiness);

            return Ok(created);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }

    [HttpPut]
    [Route("")]
    public async Task<IActionResult> Update([FromBody] ReadinessInfo readiness)
    {
        try
        {
            var updated = await _readinessService.Update(readiness);

            return Ok(updated);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _readinessService.Delete(id);

        return NoContent();
    }
}