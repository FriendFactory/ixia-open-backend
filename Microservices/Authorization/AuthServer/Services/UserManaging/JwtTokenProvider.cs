using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AuthServer.Services.EmailAuth;
using AuthServer.Services.PhoneNumberAuth;
using Common.Infrastructure;
using Common.Infrastructure.ServiceDiscovery;
using Common.Infrastructure.Utils;

namespace AuthServer.Services.UserManaging;

public interface IJwtTokenProvider
{
    Task<string> GetJwtToken(
        string appleId = null,
        string appleIdentityToken = null,
        string email = null,
        string phoneNumber = null,
        string userName = null,
        string password = null,
        string googleId = null,
        string googleIdentityToken = null
    );
}

public class HttpJwtTokenProvider(
    ServiceUrls urls,
    IdentityServerConfiguration config,
    IPhoneNumberAuthService phoneNumberAuthService,
    IEmailAuthService emailAuthService,
    IHttpClientFactory httpClientFactory
) : IJwtTokenProvider
{
    public async Task<string> GetJwtToken(
        string appleId = null,
        string appleIdentityToken = null,
        string email = null,
        string phoneNumber = null,
        string userName = null,
        string password = null,
        string googleId = null,
        string googleIdentityToken = null
    )
    {
        const string path = "/connect/token";

        var tokenUri = UriUtils.CombineUri(urls.Auth, path);

        var formData = new Dictionary<string, string>
                       {
                           ["client_id"] = config.ClientId, ["client_secret"] = config.ClientSecret, ["scope"] = config.Scopes
                       };

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            formData["grant_type"] = AuthConstants.GrantType.PhoneNumberToken;
            formData[AuthConstants.TokenRequest.PhoneNumber] = phoneNumber;
            formData[AuthConstants.TokenRequest.Token] = await phoneNumberAuthService.GenerateVerificationCode(phoneNumber);
        }
        else if (!string.IsNullOrWhiteSpace(password))
        {
            formData["grant_type"] = AuthConstants.GrantType.Password;
            formData["username"] = userName;
            formData["password"] = password;
        }
        else if (!string.IsNullOrWhiteSpace(appleId))
        {
            formData["grant_type"] = AuthConstants.GrantType.AppleAuthToken;
            formData[AuthConstants.TokenRequest.AppleId] = appleId;
            formData[AuthConstants.TokenRequest.IdentityToken] = appleIdentityToken;
        }
        else if (!string.IsNullOrWhiteSpace(googleId))
        {
            formData["grant_type"] = AuthConstants.GrantType.GoogleAuthToken;
            formData[AuthConstants.TokenRequest.GoogleId] = googleId;
            formData[AuthConstants.TokenRequest.IdentityToken] = googleIdentityToken;
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            formData["grant_type"] = AuthConstants.GrantType.EmailToken;
            formData[AuthConstants.TokenRequest.Email] = email;
            formData[AuthConstants.TokenRequest.Token] = await emailAuthService.GenerateVerificationCode(email);
        }

        var client = httpClientFactory.CreateClient();
        using var response = await client.PostAsync(tokenUri, new FormUrlEncodedContent(formData));

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw AppErrorWithStatusCodeException.BadRequest(content, "TokenError");

        return content;
    }
}