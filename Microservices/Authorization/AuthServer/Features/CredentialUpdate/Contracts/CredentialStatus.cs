namespace AuthServer.Features.CredentialUpdate.Contracts;

public class CredentialStatus
{
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public bool HasPassword { get; set; }
    public bool HasAppleId { get; set; }
    public bool HasGoogleId { get; set; }
}