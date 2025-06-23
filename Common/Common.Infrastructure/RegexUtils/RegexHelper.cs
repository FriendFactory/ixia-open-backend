using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Common.Infrastructure.RegexUtils;

public static class RegexHelper
{
    public static IReadOnlyList<string> GetMatches(string input, string pattern)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<string>();

        var findMatchesRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.Multiline);

        return findMatchesRegex.Matches(input).Where(e => e.Success).Select(e => e.Value).ToList();
    }
}