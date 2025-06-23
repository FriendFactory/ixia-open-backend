using System.Threading.Tasks;
using Frever.AdminService.Core.Services.AI;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;

namespace Frever.AdminService.Api.Controllers.AI;

[ApiController]
[Route("api/ai/metadata")]
public class AiMetadataController(IMetadataService service) : ControllerBase
{
    [HttpGet("art-style")]
    public async Task<IActionResult> GetArtStyles(ODataQueryOptions<AiArtStyle> options)
    {
        var result = await service.GetArtStyles(options);

        return Ok(result);
    }

    [HttpGet]
    [Route("prompt")]
    public async Task<IActionResult> GetLlmPrompts(ODataQueryOptions<AiLlmPrompt> options)
    {
        var result = await service.GetLlmPrompts(options);

        return Ok(result);
    }

    [HttpGet]
    [Route("workflow")]
    public async Task<IActionResult> GetAiWorkflowMetadata(ODataQueryOptions<AiWorkflowMetadata> options)
    {
        var result = await service.GetAiWorkflowMetadata(options);
        return Ok(result);
    }

    [HttpPost("art-style")]
    public async Task<IActionResult> SaveArtStyle(AiArtStyle model)
    {
        await service.SaveArtStyle(model);
        return Ok();
    }

    [HttpPost]
    [Route("prompt")]
    public async Task<IActionResult> SaveLlmPrompt(AiLlmPrompt model)
    {
        await service.SaveLlmPrompt(model);
        return Ok();
    }

    [HttpPost]
    [Route("workflow")]
    public async Task<IActionResult> SaveWorfklowMetadata(AiWorkflowMetadata model)
    {
        await service.SaveWorkflowMetadata(model);
        return Ok();
    }
}