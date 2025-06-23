using System.Threading.Tasks;
using Frever.AdminService.Core.Services.CreatePage;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;

namespace Frever.AdminService.Api.Controllers;

[ApiController]
[Route("api/create-page/moderation")]
public class CreatePageController(ICreatePageService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCreatePageRows(ODataQueryOptions<CreatePageRowResponse> options)
    {
        var result = await service.GetCreatePageRows(options);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCreatePageRow(long id)
    {
        var result = await service.GetCreatePageRow(id);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> SaveCreatePageRow(CreatePageRowRequest request)
    {
        await service.SaveCreatePageRow(request);

        return NoContent();
    }
}