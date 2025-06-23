namespace Common.Infrastructure.MusicProvider;

public class MusicProviderOAuthSettings
{
    public static readonly string[] BaseUrl = ["https://api.7digital.com/", "https://previews.7digital.com/"];
    public string OAuthConsumerKey { get; set; }
    public string OAuthSignatureMethod { get; set; }
    public string OAuthVersion { get; set; }
    public string OAuthConsumerSecret { get; set; }
}