using System.Threading.Tasks;

namespace AssetServer.Services;

public interface ICloudFrontService
{
    string CreateCdnUrl(string filePath);
    Task<string> SignUrl(string cdnUrl);
}