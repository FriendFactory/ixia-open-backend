using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Frever.Client.Shared.Files;

public interface IExternalFileDownloader
{
    Task<byte[]> Download(string url);
}

public class ExternalFileDownloader(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory) : IExternalFileDownloader
{
    private static readonly int MaxLength = (int) Math.Pow(1024.0, 3.0); // 1Gb

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly ILogger _log = loggerFactory.CreateLogger("Ixia.AiContent");

    public async Task<byte[]> Download(string url)
    {
        _log.LogInformation("Start downloading {url}", url);

        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength;
        if (contentLength != null && contentLength > MaxLength)
        {
            _log.LogError("Content length is too long: {current} but {max} allowed", contentLength, MaxLength);
            throw new InvalidOperationException($"File {url} is too big");
        }

        var data = await response.Content.ReadAsByteArrayAsync();
        if (data.Length > MaxLength)
        {
            _log.LogError("Content length is too long: {current} but {max} allowed", data.Length, MaxLength);
            throw new InvalidOperationException($"File {url} is too big");
        }

        _log.LogInformation("Downloaded {n} bytes from {url}", data.Length, url);

        return data;
    }
}