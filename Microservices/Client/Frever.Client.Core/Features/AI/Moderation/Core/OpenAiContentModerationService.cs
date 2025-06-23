using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Frever.Client.Core.Features.AI.Moderation.External.OpenAi;
using Frever.Client.Shared.Files;
using Frever.Shared.MainDb.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Frever.Client.Core.Features.AI.Moderation.Core;

public class OpenAiContentModerationService(
    IOpenAiClient openAiClient,
    IFileStorageBackend fileStorageBackend,
    ILoggerFactory loggerFactory
) : IAiContentModerationService
{
    private readonly ILogger log = loggerFactory.CreateLogger("Ixia.Client.Moderation");

    public async Task<AiContentItemModeration> ModerateImage(
        string fileKey,
        string itemId,
        Dictionary<string, decimal> customCategoryWeights
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        using var logScope = log.BeginScope(
            "{rid}: Moderate image: Key={key} Item={itemId}:",
            Guid.NewGuid().ToString("N"),
            fileKey,
            itemId
        );

        var url = fileStorageBackend.MakeCdnUrl(fileKey, true);
        log.LogInformation("Image URL={url}", url);

        var response = await openAiClient.Moderate(null, new ImageUrlInput {ImageUrl = new ImageUrl {Url = url}});

        return ToResponse(
            response,
            itemId,
            "image",
            fileKey,
            customCategoryWeights
        );
    }

    public async Task<AiContentItemModeration> ModerateText(string text, string itemId, Dictionary<string, decimal> customCategoryWeights)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);

        using var logScope = log.BeginScope("{rid}: Moderate text: Item={itemId}:", Guid.NewGuid().ToString("N"), itemId);

        log.LogInformation("Text to moderate: {text}", text);

        var response = await openAiClient.Moderate(new TextInput {Text = text}, null);

        return ToResponse(
            response,
            itemId,
            "text",
            text,
            customCategoryWeights
        );
    }

    private AiContentItemModeration ToResponse(
        OpenAiModerationResponse response,
        string itemId,
        string type,
        string value,
        Dictionary<string, decimal> customCategoryWeights
    )
    {
        if (response == null)
            throw new ArgumentNullException(nameof(response));
        if (itemId == null)
            throw new ArgumentNullException(nameof(itemId));
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(type));
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));

        customCategoryWeights ??= new();

        var result = new AiContentItemModeration
                     {
                         Response = response,
                         ContentId = itemId,
                         MediaType = type,
                         Value = value,
                         // CustomCategoryWeights = customCategoryWeights,
                         IsPassed = IsModerationPassed(response, customCategoryWeights)
                     };

        return result;
    }

    private bool IsModerationPassed(OpenAiModerationResponse response, Dictionary<string, decimal> customCategoryWeights)
    {
        if (response.Results.Any(r => r.Flagged))
            return false;

        var hasCategoryPassedThreshold = response.Results.Any(
            r => customCategoryWeights.Any(
                cw =>
                {
                    if (r.CategoryScores.TryGetValue(cw.Key, out var actualWeight))
                        return actualWeight > cw.Value;

                    return false;
                }
            )
        );

        return !hasCategoryPassedThreshold;
    }
}