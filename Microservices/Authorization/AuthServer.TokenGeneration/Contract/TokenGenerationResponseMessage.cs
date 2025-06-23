using System;

namespace AuthServer.TokenGeneration.Contract;

public class TokenGenerationResponseMessage
{
    public Guid CorrelationId { get; set; }

    public bool Ok { get; set; }

    public string Jwt { get; set; }

    public string ErrorMessage { get; set; }
}