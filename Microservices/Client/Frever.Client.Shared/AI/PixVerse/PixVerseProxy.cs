using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AuthServerShared;
using Common.Infrastructure.Utils;
using Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Frever.Client.Shared.AI.PixVerse;

public interface IPixVerseProxy
{
    Task<PixVerseImageUploadResponse> UploadImage(IFormFile file);
    Task<PixVerseProgressResponse> ImageToVideo(string request);
    Task<PixVerseProgressResponse> TextToVideo(string request);
    Task<PixVerseResultResponse> GetResult(string videoId);
}

public class PixVerseImageUploadResponse
{
    public bool Ok { get; set; }
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
    public string ImageId { get; set; }
}

public class PixVerseProgressResponse
{
    public bool Ok { get; set; }
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
    public string VideoId { get; set; }
    public long AiContentId { get; set; }
}

public class PixVerseResultResponse
{
    public bool Ok { get; set; }
    public bool IsReady { get; set; }
    public string ErrorMessage { get; set; }
    public string Url { get; set; }
}

public class PixVerseRequest
{
    [JsonProperty("duration")] public int Duration { get; set; }
    [JsonProperty("img_id")] public long ImgId { get; set; }
    [JsonProperty("model")] public string Model { get; set; } = "v4.5";
    [JsonProperty("prompt")] public string Prompt { get; set; }
    [JsonProperty("quality")] public string Quality { get; set; } = "360p";

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}

public class PixVerseProxy(
    IHttpClientFactory httpClientFactory,
    PixVerseSettings settings,
    UserInfo currentUser,
    ILogger<PixVerseProxy> logger
) : IPixVerseProxy
{
    private const string ApiHost = "https://app-api.pixverse.ai/openapi/v2";
    private const int SuccessfulStatus = 1;
    private const int GeneratingStatus = 5;
    private const int ImageModerationErrorCode = 500054;
    private const int TextModerationErrorCode = 500063;

    public async Task<PixVerseImageUploadResponse> UploadImage(IFormFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        using var scope = logger.BeginScope("PixVerse::UploadImage(group={}):", currentUser?.UserMainGroupId ?? 1);

        var url = new Uri(UriUtils.CombineUri(ApiHost, "image/upload"));
        using var httpRequest = new HttpRequestMessage();
        httpRequest.Headers.Add("api-key", settings.PixVerseApiKey);
        httpRequest.Headers.Add("ai-trace-id", Guid.NewGuid().ToString());
        httpRequest.Method = HttpMethod.Post;
        httpRequest.RequestUri = url;

        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        content.Add(fileContent, "image", file.FileName);

        httpRequest.Content = content;

        using var httpClient = httpClientFactory.CreateClient();
        var httpResponse = await httpClient.SendAsync(httpRequest);
        var body = await httpResponse.Content.ReadAsStringAsync();

        logger.LogInformation("HTTP Response received, status code={StatusCode} body={Body}", httpResponse.StatusCode, body);

        if (!httpResponse.IsSuccessStatusCode)
        {
            logger.LogError("Error receiving response: {StatusCode}", httpResponse.StatusCode);
            var jsonObj = JObject.Parse(body);
            if (jsonObj["ErrCode"]?.ToObject<int>() == ImageModerationErrorCode)
                return new PixVerseImageUploadResponse
                       {
                           ErrorCode = ErrorCodes.ModerationError, ErrorMessage = "Image uploading has not been passed moderation"
                       };

            return new PixVerseImageUploadResponse {ErrorMessage = "Error uploading image"};
        }

        var resp = ParseResponse(body, "img_id");
        return resp != null
                   ? new PixVerseImageUploadResponse {Ok = true, ImageId = resp}
                   : new PixVerseImageUploadResponse {ErrorMessage = "Error uploading image"};
    }

    public async Task<PixVerseProgressResponse> ImageToVideo(string request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var scope = logger.BeginScope("PixVerse::ImageToVideo(group={}):", currentUser?.UserMainGroupId ?? 1);

        logger.LogInformation("Parameters: {}", request);

        var url = new Uri(UriUtils.CombineUri(ApiHost, "video/img/generate"));
        using var httpRequest = new HttpRequestMessage();
        httpRequest.Headers.Add("api-key", settings.PixVerseApiKey);
        httpRequest.Headers.Add("ai-trace-id", Guid.NewGuid().ToString());
        httpRequest.Method = HttpMethod.Post;
        httpRequest.RequestUri = url;
        httpRequest.Content = new StringContent(request, Encoding.UTF8, "application/json");

        using var httpClient = httpClientFactory.CreateClient();
        var httpResponse = await httpClient.SendAsync(httpRequest);
        var body = await httpResponse.Content.ReadAsStringAsync();

        logger.LogInformation("HTTP Response received, status code={StatusCode} body={Body}", httpResponse.StatusCode, body);

        if (!httpResponse.IsSuccessStatusCode)
        {
            logger.LogError("Error receiving response: {StatusCode}", httpResponse.StatusCode);

            var jsonObj = JObject.Parse(body);
            if (jsonObj["ErrCode"]?.ToObject<int>() == TextModerationErrorCode)
                return new PixVerseProgressResponse
                       {
                           ErrorCode = ErrorCodes.ModerationError, ErrorMessage = "Prompt has not been passed moderation"
                       };
            return new PixVerseProgressResponse {ErrorMessage = "Error video generation"};
        }

        var resp = ParseResponse(body, "video_id");
        return resp != null
                   ? new PixVerseProgressResponse {Ok = true, VideoId = resp}
                   : new PixVerseProgressResponse {ErrorMessage = "Error video generation"};
    }

    public async Task<PixVerseProgressResponse> TextToVideo(string request)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var scope = logger.BeginScope("PixVerse::TextToVideo(group={}):", currentUser?.UserMainGroupId ?? 1);

        logger.LogInformation("Parameters: {}", request);

        var url = new Uri(UriUtils.CombineUri(ApiHost, "video/text/generate"));
        using var httpRequest = new HttpRequestMessage();
        httpRequest.Headers.Add("api-key", settings.PixVerseApiKey);
        httpRequest.Headers.Add("ai-trace-id", Guid.NewGuid().ToString());
        httpRequest.Method = HttpMethod.Post;
        httpRequest.RequestUri = url;
        httpRequest.Content = new StringContent(request, Encoding.UTF8, "application/json");

        using var httpClient = httpClientFactory.CreateClient();
        var httpResponse = await httpClient.SendAsync(httpRequest);
        var body = await httpResponse.Content.ReadAsStringAsync();

        logger.LogInformation("HTTP Response received, status code={StatusCode} body={Body}", httpResponse.StatusCode, body);

        if (!httpResponse.IsSuccessStatusCode)
        {
            logger.LogError("Error receiving response: {StatusCode}", httpResponse.StatusCode);

            var jsonObj = JObject.Parse(body);
            if (jsonObj["ErrCode"]?.ToObject<int>() == TextModerationErrorCode)
                return new PixVerseProgressResponse
                       {
                           ErrorCode = ErrorCodes.ModerationError, ErrorMessage = "Prompt has not been passed moderation"
                       };
            return new PixVerseProgressResponse {ErrorMessage = "Error video generation"};
        }

        var resp = ParseResponse(body, "video_id");
        return resp != null
                   ? new PixVerseProgressResponse {Ok = true, VideoId = resp}
                   : new PixVerseProgressResponse {ErrorMessage = "Error video generation"};
    }

    public async Task<PixVerseResultResponse> GetResult(string videoId)
    {
        using var scope = logger.BeginScope("PixVerse::GetResult(group={}, video={}):", currentUser?.UserMainGroupId ?? 1, videoId);

        var url = new Uri(UriUtils.CombineUri(ApiHost, $"video/result/{videoId}"));
        using var httpRequest = new HttpRequestMessage();
        httpRequest.Headers.Add("api-key", settings.PixVerseApiKey);
        httpRequest.Headers.Add("ai-trace-id", Guid.NewGuid().ToString());
        httpRequest.Method = HttpMethod.Get;
        httpRequest.RequestUri = url;

        using var httpClient = httpClientFactory.CreateClient();
        var httpResponse = await httpClient.SendAsync(httpRequest);
        var body = await httpResponse.Content.ReadAsStringAsync();

        logger.LogInformation("HTTP Response received, status code={StatusCode} body={Body}", httpResponse.StatusCode, body);

        if (!httpResponse.IsSuccessStatusCode)
        {
            logger.LogError("Error receiving response: {StatusCode}", httpResponse.StatusCode);
            return new PixVerseResultResponse {ErrorMessage = "Error getting generation result"};
        }

        var jsonObj = JObject.Parse(body);
        if (jsonObj["ErrCode"]?.ToObject<int>() != 0)
        {
            logger.LogError("Error receiving response: {ErrMsg}", jsonObj["ErrMsg"]?.ToString());
            return new PixVerseResultResponse {ErrorMessage = "Error getting generation result"};
        }

        var status = jsonObj["Resp"]?["status"]?.ToObject<int>();
        if (status == null || (status != GeneratingStatus && status != SuccessfulStatus))
            return new PixVerseResultResponse {ErrorMessage = "Error getting generation result"};

        var videoUrl = jsonObj["Resp"]?["url"]?.ToString();
        return new PixVerseResultResponse {Ok = true, IsReady = status == SuccessfulStatus, Url = WebUtility.UrlDecode(videoUrl)};
    }

    private string ParseResponse(string body, string propName)
    {
        var jsonObj = JObject.Parse(body);

        if (jsonObj["ErrCode"]?.ToObject<int>() == 0)
            return jsonObj["Resp"]?[propName]?.ToString();

        logger.LogError("Error receiving response: {ErrMsg}", jsonObj["ErrMsg"]?.ToString());
        return null;
    }
}