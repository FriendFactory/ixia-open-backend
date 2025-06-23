using System.Threading.Tasks;

namespace AuthServer.TokenGeneration;

public interface ITokenGenerationClient
{
    Task<TokenGenerationResult> GenerateTokenByGroupId(long groupId);
}