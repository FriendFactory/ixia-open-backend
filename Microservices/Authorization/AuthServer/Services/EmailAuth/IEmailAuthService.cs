using System.Threading.Tasks;

namespace AuthServer.Services.EmailAuth;

public interface IEmailAuthService
{
    Task SendEmailVerification(VerifyEmailRequest request);
    Task<bool> ValidateVerificationCode(string email, string verificationCode);
    Task<string> GenerateVerificationCode(string email);
    Task SendParentEmailVerification(VerifyEmailRequest request);
    Task<bool> ValidateParentEmailCode(string email, string verificationCode);
}