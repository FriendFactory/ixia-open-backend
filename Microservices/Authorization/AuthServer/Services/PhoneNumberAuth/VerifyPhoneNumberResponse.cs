namespace AuthServer.Services.PhoneNumberAuth;

public class VerifyPhoneNumberResponse
{
    public bool IsSuccessful { get; set; }

    public string ErrorCode { get; set; }

    public string ErrorMessage { get; set; }

    public int SecondsTillNextRetry { get; set; }
}