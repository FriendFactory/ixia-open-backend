using System.Collections.Generic;
using System.Threading.Tasks;
using Frever.Shared.MainDb.Entities;

namespace Frever.Video.Core.Features.Comments;

public interface IMentionService
{
    Task<List<Mention>> GetMentions(string commentText);
}