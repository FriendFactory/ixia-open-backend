using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;

namespace Common.Infrastructure;

/// <summary>
///     To generate key bytes from *.p8 downloaded from apple need to run next commands:
///     openssl pkcs8 -nocrypt -in AuthKey_Y988D2ZBY7.p8  -out Y988D2ZBY7.pem
///     cat Y988D2ZBY7.pem | base64 | pbcopy
/// </summary>
public class AppleApiClient(IHttpClientFactory httpClientFactory, string issuer, string keyId, byte[] key)
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    private readonly string _issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
    private readonly byte[] _key = key ?? throw new ArgumentNullException(nameof(key));
    private readonly string _keyID = keyId ?? throw new ArgumentNullException(nameof(keyId));

    public async Task<HttpResponseMessage> CallApple(HttpMethod method, Uri uri, HttpContent content = null)
    {
        ArgumentNullException.ThrowIfNull(uri);

        using var client = _httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage();
        request.Method = method;
        request.RequestUri = uri;
        request.Content = content;

        var jwt = await GenerateToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        return await client.SendAsync(request);
    }

    public async Task<HttpResponseMessage> CallAppleDownload(HttpMethod method, Uri uri, HttpContent content = null)
    {
        ArgumentNullException.ThrowIfNull(uri);

        using var client = _httpClientFactory.CreateClient();

        using var request = new HttpRequestMessage();
        request.Method = method;
        request.RequestUri = uri;
        request.Content = content;

        var jwt = await GenerateToken();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/a-gzip"));

        return await client.SendAsync(request);
    }

    // https://developer.apple.com/documentation/appstoreconnectapi/generating_tokens_for_api_requests
    private Task<string> GenerateToken()
    {
        var securityKey = new ECDsaSecurityKey(GetECDsa()) {KeyId = _keyID};
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256);
        var header = new JwtHeader(creds);

        var payload = new JwtPayload
                      {
                          {"iss", _issuer},
                          {"iat", DateTimeOffset.Now.ToUnixTimeSeconds()},
                          {"exp", DateTimeOffset.Now.AddSeconds(60).ToUnixTimeSeconds()},
                          {"aud", "appstoreconnect-v1"}
                      };

        var token = new JwtSecurityToken(header, payload);
        var handler = new JwtSecurityTokenHandler();

        return Task.FromResult(handler.WriteToken(token));
    }

    private ECDsa GetECDsa()
    {
        var ecPrivateKeyParameters = (ECPrivateKeyParameters) new PemReader(new StringReader(Encoding.UTF8.GetString(_key))).ReadObject();

        var x = ecPrivateKeyParameters.Parameters.G.AffineXCoord.GetEncoded();
        var y = ecPrivateKeyParameters.Parameters.G.AffineYCoord.GetEncoded();
        var d = ecPrivateKeyParameters.D.ToByteArrayUnsigned();

        // Convert the BouncyCastle key to a Native Key.
        var msEcp = new ECParameters {Curve = ECCurve.NamedCurves.nistP256, Q = {X = x, Y = y}, D = d};
        return ECDsa.Create(msEcp);
    }
}