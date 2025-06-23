namespace AuthServerShared.Validation;

public class ValidationResponse
{
    public bool IsValid { get; set; }

    public string ValidationError { get; set; }
}