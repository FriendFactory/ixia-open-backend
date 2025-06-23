using System;
using System.Text;
using Common.Infrastructure;
using Jose;

namespace AuthServer.Features.CredentialUpdate;

public interface ITokenProvider
{
    string GenerateToken(long groupId);
    TokenPayload ParseToken(string token);
}

public class TokenProvider : ITokenProvider
{
    private const string Secret = "f#13P38nt91p@Cfxr3hK5FlS0b@NIwLT";

    public string GenerateToken(long groupId)
    {
        try
        {
            var payload = new TokenPayload
                          {
                              GroupId = groupId, IssuedAt = DateTime.UtcNow.Ticks, ExpiredAt = DateTime.UtcNow.AddMinutes(5).Ticks
                          };

            var secret = Encoding.UTF8.GetBytes(Secret);
            return JWT.Encode(payload, secret, JwsAlgorithm.HS256);
        }
        catch (Exception)
        {
            throw AppErrorWithStatusCodeException.BadRequest("Error generating token", "ErrorGeneratingToken");
        }
    }

    public TokenPayload ParseToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var secret = Encoding.UTF8.GetBytes(Secret);
        return JWT.Decode<TokenPayload>(token, secret, JwsAlgorithm.HS256);
    }
}

public class TokenPayload
{
    public long GroupId { get; init; }
    public long ExpiredAt { get; init; }
    public long IssuedAt { get; init; }
}