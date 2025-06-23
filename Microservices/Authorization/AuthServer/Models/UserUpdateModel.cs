using System;

namespace AuthServer.Models;

public record UserUpdateModel(
    Guid IdentityServerId,
    string NickName,
    DateTime? BirthDate,
    string AppleId,
    string GoogleId,
    string Email,
    string PhoneNumber,
    string DefaultLanguage,
    string Country,
    bool IsMinor,
    bool HasPassword
);