using System.Threading.Tasks;
using AuthServer.Features.CredentialUpdate.Contracts;
using AuthServer.Features.CredentialUpdate.Models;

namespace AuthServer.Features.CredentialUpdate.Handlers;

public interface ICredentialHandler
{
    CredentialType HandlerType { get; }

    Task AddCredentials(AddCredentialsRequest request, long groupId, CredentialStatus status);

    Task<(bool IsValid, string ErrorMessage, string ErrorCode)> ValidateCurrentCredential(
        VerifyUserRequest request,
        ShortUserInfo userInfo
    );

    Task UpdateCredentials(UpdateCredentialsRequest request, long groupId, CredentialStatus status);
}