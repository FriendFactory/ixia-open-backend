namespace AuthServer.Services;

public class AuthConstants
{
    public struct TokenRequest
    {
        public const string Email = "email";
        public const string PhoneNumber = "phone_number";
        public const string Token = "verification_token";
        public const string AppleId = "apple_id";
        public const string GoogleId = "google_id";
        public const string IdentityToken = "identity_token";
    }

    public struct GrantType
    {
        public const string Password = "password";
        public const string EmailToken = "email_auth_token";
        public const string PhoneNumberToken = "phone_number_token";
        public const string AppleAuthToken = "apple_auth_token";
        public const string GoogleAuthToken = "google_auth_token";
    }
}