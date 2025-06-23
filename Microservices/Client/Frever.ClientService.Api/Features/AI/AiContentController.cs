using System;
using System.Threading.Tasks;
using FluentValidation;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Content.Input;
using Frever.ClientService.Contract.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.AI;

[Authorize]
[Route("/api/ai/content/")]
public class AiContentController(IAiGeneratedContentService aiMetadataService) : ControllerBase
{
    [HttpGet]
    [Route("drafts")]
    public async Task<IActionResult> GetDrafts(
        [FromQuery] AiGeneratedContentType? type,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10
    )
    {
        take = Math.Clamp(take, 1, 50);
        var list = await aiMetadataService.GetDrafts(type, skip, take);
        return Ok(list);
    }

    [HttpGet]
    [Route("feed")]
    public async Task<IActionResult> GetFeed(
        [FromQuery] long groupId,
        [FromQuery] AiGeneratedContentType? type,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10
    )
    {
        take = Math.Clamp(take, 1, 50);
        var list = await aiMetadataService.GetFeed(groupId, type, skip, take);
        return Ok(list);
    }

    [HttpGet]
    [Route("{id}/status")]
    public async Task<IActionResult> GetStatus([FromRoute] long id)
    {
        var content = await aiMetadataService.GetStatus(id);
        return content == null ? NotFound() : Ok(content);
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetById([FromRoute] long id)
    {
        var content = await aiMetadataService.GetById(id);

        if (content == null)
            return NotFound();

        return Ok(content);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] AiGeneratedContentInput request)
    {
        try
        {
            var saved = await aiMetadataService.SaveDraft(request);

            return Ok(saved);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> Delete([FromRoute] long id)
    {
        await aiMetadataService.Delete(id);
        return NoContent();
    }
}