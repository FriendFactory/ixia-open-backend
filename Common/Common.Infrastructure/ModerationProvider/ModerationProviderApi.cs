using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Infrastructure.EmailSending;
using Common.Infrastructure.ModerationProvider.TextModeration;
using Common.Infrastructure.ModerationProvider.VisualModeration;
using ImageMagick;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Common.Infrastructure.ModerationProvider;

internal class ModerationProviderApi : IModerationProviderApi
{
    private const string HiveEndpoint = "https://api.thehive.ai/api/v2/task/sync";

    private static readonly string[] EmailAddr;
    private readonly IEmailSendingService _emailSendingService;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ModerationProviderApi> _log;
    private readonly ModerationProviderApiSettings _settings;

    static ModerationProviderApi()
    {
        EmailAddr = [];
    }

    public ModerationProviderApi(
        IHttpClientFactory httpClientFactory,
        ILogger<ModerationProviderApi> log,
        IOptions<ModerationProviderApiSettings> options,
        IEmailSendingService emailSendingService
    )
    {
        if (options == null || options.Value == null)
            throw new ArgumentNullException(nameof(options));

        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _settings = options.Value;
        _emailSendingService = emailSendingService ?? throw new ArgumentNullException(nameof(emailSendingService));
    }


    public Task<ModerationResult> CallModerationProviderApiText(string text)
    {
        var json = JsonConvert.SerializeObject(new {text_data = text});

        return CallModerationProviderApi(json);
    }

    public async Task<ModerationResult> CallModerationProviderApi(string json)
    {
        json = Regex.Replace(json, @"\s+", " ");
        _log.LogInformation("Calling moderation provider with json {Json}", json);

        if (string.IsNullOrEmpty(json))
            return ModerationResult.DummyPassed;

        try
        {
            JToken.Parse(json);
        }
        catch (JsonReaderException e)
        {
            _log.LogInformation("Invalid JSON for text {}, with exception message {}. ", json, e.Message);
            throw new ArgumentException("Invalid JSON for text", nameof(json));
        }

        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, HiveEndpoint);
        requestMessage.Headers.Add("Authorization", $"Token {_settings.HiveTextModerationKey}");
        requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        requestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.SendAsync(requestMessage);
            var content = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var textModerationResponse = JsonConvert.DeserializeObject<TextModerationResponse>(content);
                var (passedModeration, reason) = textModerationResponse.GetModerationResult();

                var moderationResult = new ModerationResult
                                       {
                                           StatusCode = (int) response.StatusCode, PassedModeration = passedModeration, Reason = reason
                                       };
                _log.LogInformation("Moderation result:: {ModerationResult}", moderationResult);
                return moderationResult;
            }

            // always pass moderation when failed to call moderation provider
            _log.LogWarning(
                "Calling moderation provider failed, json {Json}, statusCode {Status}, response {Error}",
                json,
                response.StatusCode.ToString(),
                content
            );
            return ModerationResult.DummyPassed;
        }
        catch (Exception e)
        {
            _log.LogWarning(e, "Calling moderation provider failed with exception, text {Json}", json);
            return ModerationResult.DummyPassed;
        }
    }

    public async Task<ModerationResult> CallModerationProviderApi(byte[] input, string format, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = $"file.{format}";

        var content = new MemoryStream(input);
        if ("heic".Equals(format, StringComparison.OrdinalIgnoreCase))
        {
            using var source = new MagickImage(content);
            var newStream = new MemoryStream();
            await source.WriteAsync(newStream, MagickFormat.Jpg);
            newStream.Seek(0, SeekOrigin.Begin);
            content = newStream;
        }

        _log.LogInformation("Calling moderation provider with File size {}", input.Length);
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(90);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, HiveEndpoint);
        requestMessage.Version = HttpVersion.Version11;
        requestMessage.Headers.Add("Authorization", $"Token {_settings.HiveVisualModerationKey}");
        var multipartFormDataContent = new MultipartFormDataContent();
        multipartFormDataContent.Add(new StreamContent(content), "media", fileName);
        requestMessage.Content = multipartFormDataContent;

        try
        {
            var response = await client.SendAsync(requestMessage);
            var responseContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                var visualModerationResponse = JsonConvert.DeserializeObject<VisualModerationResponse>(responseContent);
                var (passed, reason) = visualModerationResponse.GetModerationResult();

                var moderationResult = new ModerationResult
                                       {
                                           StatusCode = (int) response.StatusCode, PassedModeration = passed, Reason = reason
                                       };
                return moderationResult;
            }

            _log.LogWarning(
                "Calling moderation provider failed, filename {FileName}, format {Format}, statusCode {Status}, errorMessage {Error}",
                fileName,
                format,
                response.StatusCode,
                responseContent
            );
            ReportModerationProviderError(fileName, format, response.StatusCode.ToString(), responseContent);
            return ModerationResult.DummyPassed;
        }
        catch (Exception e)
        {
            _log.LogWarning(e, "Calling moderation provider failed with exception, filename {FileName}, format {Format}", fileName, format);
            ReportModerationProviderError(fileName, format, "5xx", e.Message);
            return ModerationResult.DummyPassed;
        }
    }

    public async Task<ModerationResult> CallModerationProviderApi(IFormFile payload)
    {
        var format = payload.FileName.Split('.')[^1];
        var content = payload.OpenReadStream();
        var buffer = new byte[payload.Length];
        _ = await content.ReadAsync(buffer.AsMemory(0, (int) payload.Length));

        return await CallModerationProviderApi(buffer, format, payload.FileName);
    }

    private void ReportModerationProviderError(string fileName, string format, string statusCode, string errorMessage)
    {
        var emailParams = new SendEmailParams
                          {
                              To = EmailAddr,
                              Subject = "Moderation Provider Error",
                              Body =
                                  $"Moderation Provider Error for file {fileName} with format: {format}, status from provider {statusCode}, errorMessage: {errorMessage}"
                          };
        _emailSendingService.SendEmail(emailParams);
    }
}