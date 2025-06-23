using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Infrastructure.RegexUtils;
using Frever.Shared.MainDb.Entities;
using Frever.Video.Core.Features.Comments.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Frever.Video.Core.Features.Comments;

public class MentionService(IMentionRepository repo) : IMentionService
{
    public async Task<List<Mention>> GetMentions(string commentText)
    {
        var result = new List<Mention>();

        if (string.IsNullOrWhiteSpace(commentText))
            return result;

        var mentions = RegexHelper.GetMatches(commentText, RegexPatterns.Mentions);

        var groupIds = mentions.Select(e => long.TryParse(e, out var groupId) ? groupId : (long?) null)
                               .Where(e => e.HasValue)
                               .Select(e => e.Value)
                               .Distinct()
                               .ToArray();

        var groups = await repo.GetGroupByIds(groupIds).Select(e => new Mention {GroupId = e.Id, Nickname = e.NickName}).ToArrayAsync();

        result.AddRange(groups);

        return result;
    }
}