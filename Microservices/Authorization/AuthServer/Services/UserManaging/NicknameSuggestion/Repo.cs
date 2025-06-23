using System.Linq;
using Frever.Shared.MainDb;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.UserManaging.NicknameSuggestion;

public interface INicknameSuggestionRepository
{
    IQueryable<string> AllNicknameByPrefix(string prefix);
}

public class PersistentNicknameSuggestionRepository(IWriteDb db) : INicknameSuggestionRepository
{
    public IQueryable<string> AllNicknameByPrefix(string prefix)
    {
        var prefixLower = prefix.ToLowerInvariant();
        var query = db.Group.Where(g => EF.Functions.Like(g.NickName.ToLower(), prefixLower + "%")).Select(g => g.NickName);
        return query;
    }
}