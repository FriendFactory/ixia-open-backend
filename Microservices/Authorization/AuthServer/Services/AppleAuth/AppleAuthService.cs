using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace AuthServer.Services.AppleAuth;

public interface IAppleAuthService
{
    Task<string> ValidateAuthTokenAsync(string identityToken);
}

public class AppleAuthService(IHttpClientFactory httpClientFactory) : IAppleAuthService
{
    private const string Issuer = "https://appleid.apple.com";
    private const string Aud = "xxxxxxxxx";

    private JsonWebKeySet _appleAuthKeys;

    public async Task<string> ValidateAuthTokenAsync(string identityToken)
    {
        await GetAppleAuthKeysAsync();

        var data = Convert.FromBase64String(identityToken);
        var decodedToken = Encoding.UTF8.GetString(data);
        var handler = new JwtSecurityTokenHandler();
        var appleJwt = handler.ReadJwtToken(decodedToken);

        var tokenHandler = new JwtSecurityTokenHandler();
        var keyToValidate = _appleAuthKeys.Keys.First(k => k.Kid == appleJwt.Header.Kid);

        var validationParams = new TokenValidationParameters
                               {
                                   RequireExpirationTime = true,
                                   RequireSignedTokens = true,
                                   ValidateAudience = false,
                                   ValidateIssuer = false,
                                   ValidateLifetime = false,
                                   IssuerSigningKey = keyToValidate
                               };

        tokenHandler.ValidateToken(decodedToken, validationParams, out var validatedToken);

        if (validatedToken != null && appleJwt.Issuer == Issuer && appleJwt.Audiences.Any(a => a.StartsWith(Aud)))
            return appleJwt.Subject;

        return null;
    }

    private async Task GetAppleAuthKeysAsync()
    {
        var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync(new Uri("https://appleid.apple.com/auth/keys"));
        var jwksJson = await response.Content.ReadAsStringAsync();
        _appleAuthKeys = new JsonWebKeySet(jwksJson);
    }
}