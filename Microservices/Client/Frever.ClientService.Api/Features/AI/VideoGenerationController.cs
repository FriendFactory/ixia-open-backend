using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.Client.Core.Features.AI.Generation;
using Frever.Client.Core.Features.AI.Generation.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.AI;

[ApiController]
[Authorize]
[Route("/api/ai/video/generation")]
public class VideoGenerationController(IAiGenerationService service) : ControllerBase
{
    [HttpPost]
    [Route("lip-sync")]
    public async Task<IActionResult> PostLipSyncTransformation([FromBody] VideoAudioAndPromptInput request)
    {
        var result = await service.PostVideoLipSyncGeneration(request);
        return result.IsSuccess ? Ok(new GenerationResult(result.AiContentId)) : BadRequest(result);
    }

    [HttpPost]
    [Route("background/audio")]
    public async Task<IActionResult> PostVideoOnOutputTransformation([FromBody] VideoAudioAndPromptInput request)
    {
        var result = await service.PostVideoOnOutputTransformation(request);
        return result.IsSuccess ? Ok(new GenerationResult(result.AiContentId)) : BadRequest(result);
    }

    [HttpPost]
    [Route("music-gen")]
    public async Task<IActionResult> PostMusicGenTransformation([FromBody] VideoMusicGenInput request)
    {
        var result = await service.PostVideoMusicGenGeneration(request);
        return result.IsSuccess ? Ok(new GenerationResult(result.AiContentId)) : BadRequest(result);
    }

    [HttpPost]
    [Route("sfx")]
    public async Task<IActionResult> PostSfxTransformation([FromBody] VideoMusicGenInput request)
    {
        var result = await service.PostVideoSfxGeneration(request);
        return result.IsSuccess ? Ok(new GenerationResult(result.AiContentId)) : BadRequest(result);
    }

    [HttpPost]
    [Route("pix-verse/by-id")]
    public async Task<IActionResult> PostPixVerseTransformation([FromBody] PixVerseInput request)
    {
        try
        {
            var result = await service.PostVideoPixVerseGeneration(request);
            return result.Ok
                       ? Ok(new GenerationResult(result.AiContentId))
                       : BadRequest(new {Message = result.ErrorMessage, result.ErrorCode});
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return BadRequest(new {ex.Message, ex.ErrorCode, ex.StatusCode});
        }
    }

    [HttpPost]
    [Route("pix-verse")]
    public async Task<IActionResult> PostVideoPixVerseFromFileGeneration([FromForm] PixVerseFileInput request)
    {
        try
        {
            var result = await service.PostVideoPixVerseFromFileGeneration(request);
            return result.Ok
                       ? Ok(new GenerationResult(result.AiContentId))
                       : BadRequest(new {Message = result.ErrorMessage, result.ErrorCode});
        }
        catch (AppErrorWithStatusCodeException ex)
        {
            return BadRequest(new {ex.Message, ex.ErrorCode, ex.StatusCode});
        }
    }

    [HttpGet]
    [Route("result")]
    public async Task<IActionResult> GetTransformationResult([FromQuery] string key)
    {
        var result = await service.GetGenerationResult(key);
        return result.Ok ? Ok(result) : BadRequest(result.ErrorMessage);
    }
}