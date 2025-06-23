using System;
using System.Threading.Tasks;
using FluentValidation;
using Frever.Client.Core.Features.AI.Generation.Contract;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Characters;
using Frever.Client.Core.Features.AI.UserGeneratedContent.Characters.Contracts;
using Frever.Client.Core.Utils.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.AI;

[Route("/api/ai/character")]
public class AiCharacterController(IAiCharacterService service) : ControllerBase
{
    [HttpGet]
    [Route("my")]
    public async Task<IActionResult> GetMyCharacters([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 50);
        skip = Math.Clamp(skip, 0, int.MaxValue);

        var response = await service.GetMyCharacters(skip, take);
        return Ok(response);
    }

    [HttpPost]
    [Route("image/generation")]
    public async Task<IActionResult> PostCharacterImageGeneration([FromBody] AiCharacterImageGenerationInput input)
    {
        var result = await service.PostCharacterImageGeneration(input);
        if (!result.IsSuccess)
            return BadRequest(result.Message);

        return Ok(new GenerationResult(result.AiContentId));
    }

    [HttpPost]
    public async Task<IActionResult> SaveCharacter([FromBody] AiCharacterInput input)
    {
        try
        {
            await service.SaveCharacter(input);

            return Created();
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ValidationErrorResult(ex));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCharacter([FromRoute] long id)
    {
        await service.DeleteCharacter(id);

        return NoContent();
    }
}