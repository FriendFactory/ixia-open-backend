namespace Common.Infrastructure.ModerationProvider;

public class ModerationResult
{
    public static ModerationResult DummyPassed = new()
                                                 {
                                                     StatusCode = 200,
                                                     PassedModeration = true,
                                                     Reason = "Failed to call moderation provider api."
                                                 };

    public int StatusCode { get; set; }
    public string ErrorMessage { get; set; }
    public bool PassedModeration { get; set; }
    public string Reason { get; set; }

    public override string ToString()
    {
        return $"{{StatusCode: {StatusCode}, ErrorMessage: {ErrorMessage}, PassedModeration: {PassedModeration}, Reason: {Reason}}}";
    }
}