using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.AdminService.Core.Services.AiContent;
using Frever.AdminService.Core.Utils;
using Microsoft.AspNet.OData.Query;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Frever.AdminService.Api.Controllers;

[Route("/api/ai-content/moderation")]
public class AiContentController(IAiContentAdminService aiContentAdmin) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetAiContentFeed(ODataQueryOptions<AiGeneratedContentDto> options)
    {
        try
        {
            var query = aiContentAdmin.GetAiGeneratedContent();
            var result = await query.ExecuteODataRequestWithCount(options);

            foreach (var c in result.Data)
                await aiContentAdmin.InitUrls(c);

            return Ok(result);
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<ActionResult> GetAiContent([FromRoute] long id)
    {
        try
        {
            var aiContent = await aiContentAdmin.GetAiGeneratedContent().FirstOrDefaultAsync(c => c.Id == id);

            if (aiContent == null)
                return NotFound();

            await aiContentAdmin.InitUrls(aiContent);

            return Ok(aiContent);
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return StatusCode((int) ex.StatusCode, ex.Message);
        }
    }
}