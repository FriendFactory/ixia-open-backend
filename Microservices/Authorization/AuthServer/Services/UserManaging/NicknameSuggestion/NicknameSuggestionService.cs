using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.UserManaging.NicknameSuggestion;

public interface INicknameSuggestionService
{
    Task<string[]> SuggestNickname(string enteredUsername, int count);
}

public class NicknameSuggestionService(INicknameSuggestionData data, INicknameSuggestionRepository repo) : INicknameSuggestionService
{
    private const int MaxNumericIndexTryout = 10;

    public async Task<string[]> SuggestNickname(string enteredUsername, int count)
    {
        var adjRandom = new UniqueRandom();
        var nounRandom = new UniqueRandom();
        var numSuffixRandom = new UniqueRandom();

        var result = new List<string>(count);

        var tryouts = 100;

        var nouns = await data.GetNouns();
        var adjectives = await data.GetAdjectives();

        while (result.Count < count && tryouts > 0)
        {
            var adj = adjectives[adjRandom.Next(0, adjectives.Length)];
            var noun = nouns[nounRandom.Next(0, nouns.Length)];

            var candidate = adj.ToPascalCase() + noun.ToPascalCase();

            var allPrefixed =
                (await repo.AllNicknameByPrefix(candidate).Take(20).ToArrayAsync()).ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            for (var i = 0; i < MaxNumericIndexTryout; i++)
            {
                var suffix = numSuffixRandom.Next(1, 1000);
                var suffixed = candidate + suffix;
                if (allPrefixed.Contains(suffixed))
                    continue;

                if (!await repo.AllNicknameByPrefix(suffixed).AnyAsync())
                {
                    result.Add(suffixed);
                    break;
                }
            }

            tryouts--;
        }


        return result.ToArray();
    }
}