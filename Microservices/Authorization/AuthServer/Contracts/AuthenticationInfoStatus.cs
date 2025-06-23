namespace AuthServer.Contracts;

public class AuthenticationInfoStatus
{
    public AuthenticationInfoStatus()
    {
        IsValid = true;
    }

    public AuthenticationInfoStatus(string validationError, string errorCode)
    {
        ValidationError = validationError;
        ErrorCode = errorCode;
    }

    public bool IsValid { get; }
    public string ErrorCode { get; }
    public string ValidationError { get; }
    public UserAccountRegistrationErrors UserRegistrationErrors { get; set; } = new();

    public static AuthenticationInfoStatus Valid()
    {
        return new AuthenticationInfoStatus();
    }
}