using System.Threading.Tasks;

namespace AuthServer.Services.TokenGeneration;

public interface ITokenGenerationService
{
    Task<string> GenerateByGroupId(long groupId);
}