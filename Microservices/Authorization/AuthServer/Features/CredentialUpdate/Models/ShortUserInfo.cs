using System;

namespace AuthServer.Features.CredentialUpdate.Models;

public class ShortUserInfo
{
    public Guid IdentityServerId { get; init; }
    public string Email { get; init; }
    public string PhoneNumber { get; init; }
    public string AppleId { get; init; }
    public string GoogleId { get; init; }
    //TODO: remove later
    public bool IsTemporary { get; set; }
}