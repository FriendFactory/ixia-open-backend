using System.Threading.Tasks;

namespace AuthServer.Services.PhoneNumberAuth;

public interface IPhoneNumberAuthService
{
    Task<VerifyPhoneNumberResponse> SendPhoneNumberVerification(VerifyPhoneNumberRequest request);
    Task<bool> ValidateVerificationCode(string phoneNumber, string verificationCode);
    Task<string> GenerateVerificationCode(string phoneNumber);
    Task<string> FormatPhoneNumber(string phoneNumber);
}