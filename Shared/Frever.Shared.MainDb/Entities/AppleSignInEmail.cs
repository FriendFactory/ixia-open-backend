using System;

namespace Frever.Shared.MainDb.Entities;

public class AppleSignInEmail
{
    public string AppleId { get; set; }

    public string Email { get; set; }

    public DateTime CreatedAt { get; set; }
}