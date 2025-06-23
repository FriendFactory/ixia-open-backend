using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AuthServer.Contracts;
using AuthServer.Quickstart.Account;
using Frever.Shared.MainDb.Entities;

namespace AuthServer.Services.UserManaging;

public interface IUserAccountService
{
    Task<IList<Claim>> GetClaimsByIdAsync(string userId);

    Task<UserAccountRegistrationResult> RegisterAccount(RegisterUserViewModel model);

    Task UpdateAccount(long groupId, UpdateAccountRequest request);

    Task<string> RegisterTemporaryAccount(TemporaryAccountRequest request);

    Task<string> LoginWithApple(LoginWithAppleRequest model);

    Task<string> LoginWithGoogle(LoginWithGoogleRequest model);

    Task StoreEmailForAppleId(AppleEmailInfoRequest request);

    Task AssignParentEmail(long groupId, string parentEmail);

    Task RemoveParentEmail(long groupId);

    Task ConfigureParentalConsent(long groupId, ParentalConsent consent);

    Task<bool> IsLoginByEmailAvailable(string userName);

    Task SendVerificationCodeToParentEmail(long groupId);

    Task<bool> VerifyParentEmailCode(long groupId, string code);

    Task<AuthenticationInfoStatus> CheckLoginInfo(AuthenticationInfo request);

    Task<AuthenticationInfoStatus> CheckRegistrationInfo(AuthenticationInfo request);
}