using System;
using System.Threading.Tasks;
using Frever.Client.Core.Features.AI.Metadata;
using Frever.Client.Core.Features.InAppPurchases.Subscriptions;
using Frever.Client.Shared.AI.Metadata;
using Frever.ClientService.Contract.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Frever.ClientService.Api.Features.AI;

[Authorize]
[Route("/api/ai/metadata")]
public class AiMetadataController(
    IAiWorkflowMetadataService workflowMetadataService,
    IOpenAiMetadataService openAiMetadataService,
    IAiMetadataService metadataService,
    IInAppSubscriptionManager subscriptionManager
) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMetadata()
    {
        await subscriptionManager.RenewSubscriptionTokens();
        var result = await metadataService.GetMetadata();
        return Ok(result);
    }

    [HttpGet]
    [Route("workflow")]
    public async Task<IActionResult> GetAiWorkflowMetadata()
    {
        var result = await workflowMetadataService.Get();
        return Ok(result);
    }

    [HttpGet]
    [Route("open-ai/key")]
    public async Task<IActionResult> GetOpenAiKey()
    {
        var key = await openAiMetadataService.GetRandomOpenAiApiKey();
        if (!string.IsNullOrWhiteSpace(key))
            return Ok(new {ApiKey = key});

        return NotFound();
    }

    [HttpGet]
    [Route("open-ai/agent")]
    public async Task<IActionResult> GetOpenAiAgent([FromQuery] string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest();

        var agent = await openAiMetadataService.GetOpenAiAgent(key);
        if (agent == null)
            return NotFound();

        return Ok(new {agent.Agent});
    }

    [HttpPost]
    [Route("llm/prompts")]
    public async Task<IActionResult> GetLlmPromptsData([FromBody] PromptInput input)
    {
        var result = await metadataService.GetLlmPromptsData(input);
        return Ok(result);
    }

    [HttpGet("art-style")]
    public async Task<IActionResult> GetArtStyles([FromQuery] long? genderId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 50);
        skip = Math.Clamp(skip, 0, int.MaxValue);

        var response = await metadataService.GetArtStyles(genderId, skip, take);
        return Ok(response);
    }


    [HttpGet("makeup")]
    public async Task<IActionResult> GetMakeUps([FromQuery] long? categoryId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 50);
        skip = Math.Clamp(skip, 0, int.MaxValue);

        var response = await metadataService.GetMakeUps(categoryId, skip, take);
        return Ok(response);
    }

    [HttpGet("speaker-mode")]
    public async Task<IActionResult> GetSpeakerModes([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 50);
        skip = Math.Clamp(skip, 0, int.MaxValue);

        var response = await metadataService.GetSpeakerModes(skip, take);
        return Ok(response);
    }

    [HttpGet("language-mode")]
    public async Task<IActionResult> GetLanguageModes([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        take = Math.Clamp(take, 1, 50);
        skip = Math.Clamp(skip, 0, int.MaxValue);

        var response = await metadataService.GetLanguageModes(skip, take);
        return Ok(response);
    }
}