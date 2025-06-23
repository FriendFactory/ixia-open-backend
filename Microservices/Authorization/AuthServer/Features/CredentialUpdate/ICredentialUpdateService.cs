using System.Threading.Tasks;
using AuthServer.Contracts;
using AuthServer.Features.CredentialUpdate.Contracts;

namespace AuthServer.Features.CredentialUpdate;

public interface ICredentialUpdateService
{
    Task<CredentialStatus> GetCredentialStatus();
    Task VerifyCredentials(VerifyCredentialRequest request);
    Task AddCredentials(AddCredentialsRequest request);
    Task<VerifyUserResponse> VerifyUser(VerifyUserRequest request);
    Task UpdateCredentials(UpdateCredentialsRequest request);
    Task<UpdateUserNameResponse> UpdateUserName(UpdateUserNameRequest request);
}