using System;
using System.Threading.Tasks;
using Common.Infrastructure;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;

namespace AuthServer.Services.GoogleAuth;

public interface IGoogleAuthService
{
    Task<string> ValidateAuthTokenAsync(string identityToken);
}

public class GoogleAuthService(ILoggerFactory loggerFactory) : IGoogleAuthService
{
    private const string Aud = "xxxxxxxxx";

    private readonly ILogger _logger = loggerFactory.CreateLogger("Frever.Auth.GoogleAuthService");

    public async Task<string> ValidateAuthTokenAsync(string identityToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identityToken);

        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings {Audience = new[] {Aud}};
            payload = await GoogleJsonWebSignature.ValidateAsync(identityToken, settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google Identity Token");
            throw AppErrorWithStatusCodeException.BadRequest("Error validating token", "GoogleTokenValidationError");
        }

        return payload?.Subject;
    }
}