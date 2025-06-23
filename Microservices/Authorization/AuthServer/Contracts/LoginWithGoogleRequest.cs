namespace AuthServer.Contracts
{
    public class LoginWithGoogleRequest
    {
        public string IdentityToken { get; set; }
        public string GoogleId { get; set; }
    }
}