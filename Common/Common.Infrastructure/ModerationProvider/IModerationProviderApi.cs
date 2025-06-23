using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Common.Infrastructure.ModerationProvider;

public interface IModerationProviderApi
{
    Task<ModerationResult> CallModerationProviderApi(string json);
    Task<ModerationResult> CallModerationProviderApiText(string text);
    Task<ModerationResult> CallModerationProviderApi(IFormFile payload);
    Task<ModerationResult> CallModerationProviderApi(byte[] input, string format, string fileName);
}