namespace AuthServer.Contracts
{
    public class LoginWithAppleRequest
    {
        public string Email { get; set; }
        public string AppleIdentityToken { get; set; }
        public string AppleId { get; set; }
    }
}