using System;
using System.Collections.Generic;
using System.Linq;
using Common.Models.Files;

namespace AssetServer.Services.Storage;

public class FileTagInfo
{
    public FileTagInfo(IDictionary<string, string> tags)
    {
        Tags = tags ?? throw new ArgumentNullException(nameof(tags));

        if (tags.TryGetValue(FileTags.GROUP_ID, out var groupIdString))
            Groups = groupIdString.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                                  .Select(g => g.Trim())
                                  .Where(g => !string.IsNullOrWhiteSpace(g))
                                  .Select(long.Parse)
                                  .ToArray();

        if (!tags.TryGetValue(FileTags.LEVEL_ID, out var levelIdString))
            return;

        if (long.TryParse(levelIdString, out var levelId))
            LevelId = levelId;
    }

    public IDictionary<string, string> Tags { get; }
    public long[] Groups { get; } = [];

    public long? LevelId { get; }
}