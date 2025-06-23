using System;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Common.Infrastructure.RequestId;

public class HttpContextHeaderAccessor(IHttpContextAccessor httpContextAccessor) : IHeaderAccessor
{
    public const string XRequestIdHeader = "x-request-id";
    public const string XFreverExperiments = "x-frever-experiments";
    public const string XLocalizationVersion = "x-localization-version";
    public const string XDeviceId = "x-device-id";
    public const string XUnityVersion = "x-unity-version";

    private const string XAiContentGenerationApiKey = "x-ai-content-generation-api-key";

    private readonly IHttpContextAccessor _httpContextAccessor =
        httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    public string GetRequestId()
    {
        return _httpContextAccessor.HttpContext?.Response.Headers[XRequestIdHeader].FirstOrDefault();
    }

    public string GetRequestExperimentsHeader()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers[XFreverExperiments].FirstOrDefault();
    }

    public string GetContentGenerationApiKey()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers[XAiContentGenerationApiKey].FirstOrDefault();
    }

    public string GetUnityVersion()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers[XUnityVersion].FirstOrDefault();
    }
}