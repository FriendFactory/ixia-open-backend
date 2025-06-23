using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.Client.Core.Features.AI.Moderation;

public interface IAiContentModerationService
{
    Task<AiContentItemModeration> ModerateImage(string fileKey, string itemId, Dictionary<string, decimal> customCategoryWeights);

    Task<AiContentItemModeration> ModerateText(string text, string itemId, Dictionary<string, decimal> customCategoryWeights);
}