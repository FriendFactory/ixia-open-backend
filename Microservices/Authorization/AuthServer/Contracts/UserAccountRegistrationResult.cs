namespace AuthServer.Contracts;

public class UserAccountRegistrationResult
{
    public bool Ok { get; set; }

    public string ErrorCode { get; set; }

    public string ErrorDetails { get; set; }

    public string Message => ErrorDetails;

    public UserAccountRegistrationErrors RegistrationErrorDetails { get; set; }

    public string Jwt { get; set; }
}