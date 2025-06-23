namespace Common.Infrastructure.MusicProvider;

public class SignedRequestData(string url, string authorizationHeader)
{
    public string Url { get; } = url;
    public string AuthorizationHeader { get; } = authorizationHeader;
}