using System;

namespace AuthServer.Contracts;

public class UpdateUserNameRequest
{
    public string UserName { get; set; }
}

public class UpdateUserNameResponse
{
    public bool Ok { get; set; }
    public string ErrorCode { get; set; }
    public string ErrorDetails { get; set; }
    public DateTime? UsernameUpdateAvailableOn { get; set; }
    public UserAccountRegistrationErrors UpdateErrorDetails { get; set; }
}