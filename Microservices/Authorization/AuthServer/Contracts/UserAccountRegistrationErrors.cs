namespace AuthServer.Contracts;

public class UserAccountRegistrationErrors
{
    public bool UsernameTaken { get; set; }

    public bool UsernameLengthIncorrect { get; set; }

    public bool UsernameContainsForbiddenSymbols { get; set; }

    public bool UsernameModerationFailed { get; set; }
}