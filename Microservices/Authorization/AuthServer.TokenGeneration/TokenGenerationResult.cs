namespace AuthServer.TokenGeneration;

public class TokenGenerationResult
{
    public bool Ok { get; set; }

    public string ErrorMessage { get; set; }

    public bool TimedOut { get; set; }

    public string Jwt { get; set; }
}