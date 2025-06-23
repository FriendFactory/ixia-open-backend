using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Common.Infrastructure.MusicProvider;

internal class OAuthSignatureProvider : IOAuthSignatureProvider
{
    private readonly SortedDictionary<string, string> _baseQueryParameters;
    private readonly string _oauthConsumerKey;
    private readonly string _oauthNonceKey;
    private readonly string _oAuthSignatureKey;
    private readonly string _oauthSignatureMethodKey;
    private readonly string _oauthTimestampKey;
    private readonly string _oauthVersionKey;
    private readonly MusicProviderOAuthSettings _settings;

    public OAuthSignatureProvider(IOptions<MusicProviderOAuthSettings> options)
    {
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _oauthNonceKey = "oauth_nonce";
        _oauthTimestampKey = "oauth_timestamp";
        _oauthConsumerKey = "oauth_consumer_key";
        _oauthSignatureMethodKey = "oauth_signature_method";
        _oauthVersionKey = "oauth_version";
        _oAuthSignatureKey = "oauth_signature";
        _baseQueryParameters = new SortedDictionary<string, string>
                               {
                                   {_oauthNonceKey, GetNonce()},
                                   {_oauthTimestampKey, GetTimeStamp()},
                                   {_oauthConsumerKey, _settings.OAuthConsumerKey},
                                   {_oauthSignatureMethodKey, _settings.OAuthSignatureMethod},
                                   {_oauthVersionKey, _settings.OAuthVersion}
                               };
    }

    public SignedRequestData GetSignedRequestData(
        MusicProviderHttpMethod httpMethod,
        string baseUrl,
        SortedDictionary<string, string> queryParameters
    )
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentNullException(nameof(baseUrl));

        if (MusicProviderOAuthSettings.BaseUrl.All(u => !baseUrl.StartsWith(u)))
            throw new ArgumentException("Not a valid Music Provider", nameof(baseUrl));

        if (queryParameters == null || queryParameters.Count == 0)
            throw new ArgumentException(nameof(queryParameters));

        var parameters = BuildQueryParameters(queryParameters);
        var queryString = GetQueryString(parameters);
        var baseString = BuildBaseString(httpMethod, baseUrl, queryString);
        var signature = GetOAuthSignature(baseString);
        var url = BuildUrl(baseUrl, queryString);
        url = AddQueryParameter(url, _oAuthSignatureKey, signature);
        var authHeader = GetAuthHeader(signature);

        return new SignedRequestData(url, authHeader);
    }

    private string GetOAuthSignature(string content)
    {
        var encoder = new ASCIIEncoding();
        var hmacKey = $"{Uri.EscapeDataString(_settings.OAuthConsumerSecret)}&";
        var hmacSha1 = new HMACSHA1(encoder.GetBytes(hmacKey));
        var hash = hmacSha1.ComputeHash(encoder.GetBytes(content));
        var signature = Convert.ToBase64String(hash);
        return Uri.EscapeDataString(signature);
    }

    private string AddQueryParameter(string uri, string parameterName, string value)
    {
        return $"{uri}&{QueryParameterTemplate(parameterName, value)}";
    }

    private string BuildBaseString(MusicProviderHttpMethod httpMethod, string baseUrl, string queryString)
    {
        return $"{httpMethod.ToString().ToUpper()}&{Uri.EscapeDataString(baseUrl)}&{Uri.EscapeDataString(queryString)}";
    }

    private string BuildUrl(string baseUrl, string queryParameters)
    {
        return $"{baseUrl}?{queryParameters}";
    }

    private string GetAuthHeader(string signature)
    {
        var authHeader = new[]
                         {
                             QueryParameterTemplate(_oauthConsumerKey, _baseQueryParameters.GetValueOrDefault(_oauthConsumerKey)),
                             QueryParameterTemplate(_oauthNonceKey, _baseQueryParameters.GetValueOrDefault(_oauthNonceKey)),
                             QueryParameterTemplate(_oAuthSignatureKey, signature),
                             QueryParameterTemplate(
                                 _oauthSignatureMethodKey,
                                 _baseQueryParameters.GetValueOrDefault(_oauthSignatureMethodKey)
                             ),
                             QueryParameterTemplate(_oauthTimestampKey, _baseQueryParameters.GetValueOrDefault(_oauthTimestampKey)),
                             QueryParameterTemplate(_oauthVersionKey, _baseQueryParameters.GetValueOrDefault(_oauthVersionKey))
                         };
        return string.Join(", ", authHeader);
    }

    private SortedDictionary<string, string> BuildQueryParameters(SortedDictionary<string, string> queryParameters)
    {
        var parameters = new SortedDictionary<string, string>(_baseQueryParameters);

        foreach (var requiredParameter in queryParameters)
            parameters.Add(requiredParameter.Key, requiredParameter.Value);

        return parameters;
    }

    private string GetQueryString(SortedDictionary<string, string> parameters)
    {
        return string.Join("&", parameters.Select(e => QueryParameterTemplate(e.Key, e.Value)));
    }

    private string QueryParameterTemplate(string parameterName, string value)
    {
        return $"{parameterName}={value}";
    }

    private string GetTimeStamp()
    {
        return Convert.ToInt64(
                           (DateTime.UtcNow - new DateTime(
                                1970,
                                1,
                                1,
                                0,
                                0,
                                0
                            )).TotalSeconds
                       )
                      .ToString(CultureInfo.InvariantCulture);
    }

    private string GetNonce()
    {
        var result = new StringBuilder();
        var rnd = new Random();

        result.Append(rnd.Next(1, 9));

        for (var i = 1; i < 9; i++)
            result.Append(rnd.Next(0, 9));

        return result.ToString();
    }
}