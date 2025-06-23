using System.Threading.Tasks;
using Frever.Client.Core.Features.AI.Generation;
using Frever.Client.Core.Features.AI.Generation.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.AI;

[ApiController]
[Authorize]
[Route("/api/ai/image/generation")]
public class ImageGenerationController(IAiGenerationService service) : ControllerBase
{
    [HttpPost]
    [Route("text-to-image")]
    public async Task<IActionResult> PostTextToImageGeneration([FromBody] ImageGenerationInput request)
    {
        var result = await service.PostTextToImageGeneration(request);
        return result.IsSuccess ? Ok(new GenerationResult(result.AiContentId)) : BadRequest(result);
    }

    [HttpPost]
    [Route("image-to-image")]
    public async Task<IActionResult> PostImageToImageGeneration([FromBody] ImageGenerationInput request)
    {
        var result = await service.PostImageToImageGeneration(request);
        return result.IsSuccess ? Ok(new GenerationResult(result.AiContentId)) : BadRequest(result);
    }

    [HttpPost]
    [Route("style")]
    public async Task<IActionResult> PostStyleGeneration([FromBody] ImageGenerationInput request)
    {
        var result = await service.PostImageStyleGeneration(request);
        return result.IsSuccess ? Ok(new GenerationResult(result.AiContentId)) : BadRequest(result);
    }

    [HttpPost]
    [Route("makeup/{id}")]
    public async Task<IActionResult> PostMakeUpGeneration([FromRoute] long id, [FromBody] ImageGenerationInput request)
    {
        var result = await service.PostImageMakeUpGeneration(id, request);
        return result.IsSuccess ? Ok(new GenerationResult(result.AiContentId)) : BadRequest(result);
    }

    [HttpPost]
    [Route("wardrobe")]
    public async Task<IActionResult> PostWardrobeGeneration([FromBody] ImageGenerationInput request)
    {
        var result = await service.PostImageWardrobeGeneration(request);
        return result.IsSuccess ? Ok(new GenerationResult(result.AiContentId)) : BadRequest(result);
    }

    [HttpPost]
    [Route("lip-sync")]
    public async Task<IActionResult> PostLipSyncGeneration([FromBody] ImageAudioAndPromptInput request)
    {
        var result = await service.PostImageLipSyncGeneration(request);
        return result.IsSuccess ? Ok(new GenerationResult(result.AiContentId)) : BadRequest(result);
    }

    [HttpPost]
    [Route("background/audio")]
    public async Task<IActionResult> PostBackgroundAudioGeneration([FromBody] ImageAudioAndPromptInput request)
    {
        var result = await service.PostImageBackgroundAudioGeneration(request);
        return result.IsSuccess ? Ok(new GenerationResult(result.AiContentId)) : BadRequest(result);
    }

    [HttpGet]
    [Route("result")]
    public async Task<IActionResult> GetTransformationResult([FromQuery] string key)
    {
        var result = await service.GetGenerationResult(key);
        return result.Ok ? Ok(result) : BadRequest(result.ErrorMessage);
    }
}