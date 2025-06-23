using FluentValidation;

namespace Frever.Client.Shared.AI.PixVerse;

public class PixVerseSettings
{
    public string PixVerseApiKey { get; set; }

    public void Validate()
    {
        var validator = new InlineValidator<PixVerseSettings>();
        validator.ValidateAndThrow(this);
    }
}