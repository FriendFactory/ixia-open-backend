using System;

namespace AuthServer.TokenGeneration.Contract;

public class TokenGenerationRequestMessage
{
    public Guid CorrelationId { get; set; }

    public long GroupId { get; set; }
}