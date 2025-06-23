using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Common.Infrastructure.ModerationProvider.TextModeration;

internal class TextModerationResponse
{
    private const int DefaultTextContentSeverityLevel = 2;

    private readonly string[] _ignoredTextModerationClasses = {"gibberish"};

    private readonly Dictionary<string, int> _moderationClassToScore = new()
                                                                       {
                                                                           {"sexual", 1},
                                                                           {"hate", 1},
                                                                           {"violence", 2},
                                                                           {"bullying", 1},
                                                                           {"drugs", 2},
                                                                           {"child_exploitation", 1},
                                                                           {"spam", 1},
                                                                           {"promotions", 1},
                                                                           {"weapons", 1},
                                                                           {"phone_number", 1},
                                                                           {"redirection", 1}
                                                                       };

    [JsonProperty("status")] private List<Status> Status { get; set; }

    public (bool, string) GetModerationResult()
    {
        var status = Status.First();
        foreach (var output in status.response.output)
        {
            var classes = output.classes;
            var list = classes.Where(x => !_ignoredTextModerationClasses.Contains(x.@class) && ScoreIsAboveThreshold(x.@class, x.score))
                              .ToList();
            if (list.Any())
                return (false, list.First().@class);
        }

        var pii = ContainPersonalIdentifiableInformation();
        if (pii.Item1)
            return (false, "Contains Personal Identifiable Information. Type: " + pii.Item2);

        var custom = ContainCustomClasses();
        if (custom.Item1)
            return (false, "Contains " + custom.Item2);

        return (true, string.Empty);
    }

    private (bool, string) ContainPersonalIdentifiableInformation()
    {
        var status = Status.First();
        if (status.response.pii_entities == null)
            return (false, string.Empty);

        return status.response.pii_entities.Count > 0 ? (true, status.response.pii_entities.First().type) : (false, string.Empty);
    }

    private (bool, string) ContainCustomClasses()
    {
        var status = Status.First();
        if (status.response.custom_classes == null)
            return (false, string.Empty);

        return status.response.custom_classes.Count > 0 ? (true, status.response.custom_classes.First().@class) : (false, string.Empty);
    }

    private bool ScoreIsAboveThreshold(string @class, int score)
    {
        if (_moderationClassToScore.TryGetValue(@class, out var value))
            return score > value;

        return score > DefaultTextContentSeverityLevel;
    }
}