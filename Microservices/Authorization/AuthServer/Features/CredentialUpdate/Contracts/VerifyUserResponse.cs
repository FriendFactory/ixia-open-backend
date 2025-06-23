using System;

namespace AuthServer.Features.CredentialUpdate.Contracts;

public class VerifyUserResponse
{
    public bool IsSuccessful { get; set; }
    public string VerificationToken { get; set; }
    public string ErrorCode { get; set; }
    public string ErrorMessage { get; set; }

    public VerifyUserResponse(string errorMessage, string errorCode)
    {
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    public VerifyUserResponse(string verificationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verificationToken);

        IsSuccessful = true;
        VerificationToken = verificationToken;
    }
}