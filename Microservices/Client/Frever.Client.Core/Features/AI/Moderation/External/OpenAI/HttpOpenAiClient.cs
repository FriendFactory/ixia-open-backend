using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Common.Infrastructure;
using Frever.Client.Core.Features.AI.Metadata;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.AI.Moderation.External.OpenAi;

public class HttpOpenAiClient(
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory,
    IOpenAiMetadataService openAiMetadataService
) : IOpenAiClient
{
    private static readonly Uri OpenAiModerationEndpoint = new Uri("https://api.openai.com/v1/moderations");
    private static readonly string OpenAiModerationModel = "omni-moderation-latest";

    private readonly HttpClient http = httpClientFactory.CreateClient();
    private readonly ILogger log = loggerFactory.CreateLogger("Ixia.External.OpenAi.Moderation");

    public async Task<OpenAiModerationResponse> Moderate(TextInput text, ImageUrlInput image)
    {
        if (text == null && image == null)
            throw new ArgumentNullException(nameof(text), "Text input must be provided");

        using var logScope = log.BeginScope("Moderation {ruid}:: ", Guid.NewGuid().ToString("N"));

        var openAiKey = await openAiMetadataService.GetRandomOpenAiApiKey();
        if (openAiKey == null)
        {
            log.LogError("Can't obtain Open AI key");
            throw new AppErrorWithStatusCodeException("No Open AI key available", HttpStatusCode.InternalServerError);
        }

        var openAiRequest = new OpenAiModerationRequest
                            {
                                Model = OpenAiModerationModel,
                                Input = new ModerationInputBase[] {text, image}.Where(m => m != null).ToArray()
                            };
        var requestJson = JsonConvert.SerializeObject(openAiRequest);

        log.LogInformation("Moderation request JSON: {json}", requestJson);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, OpenAiModerationEndpoint);
        httpRequest.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", openAiKey);

        using var response = await http.SendAsync(httpRequest);

        log.LogInformation("OpenAI responded with Status Code = {sc}", response.StatusCode);

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        log.LogInformation("Moderation response JSON: {rj}", responseBody);

        return JsonConvert.DeserializeObject<OpenAiModerationResponse>(responseBody);
    }
}