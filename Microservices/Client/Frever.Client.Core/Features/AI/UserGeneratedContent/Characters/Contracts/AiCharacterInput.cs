using FluentValidation;

namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Characters.Contracts;

public class AiCharacterInput
{
    public long GenderId { get; set; }
    public long ArtStyleId { get; set; }
    public int Age { get; set; }
    public string Ethnicity { get; set; }
    public string HairStyle { get; set; }
    public string HairColor { get; set; }
    public string FashionStyle { get; set; }
    public string Name { get; set; }
    public string Interests { get; set; }
    public string Description { get; set; }
    public AiCharacterImageInput Image { get; set; }
}

public class AiCharacterValidator : AbstractValidator<AiCharacterInput>
{
    public AiCharacterValidator(IValidator<AiCharacterImageInput> imageValidator)
    {
        RuleFor(m => m.Image).NotEmpty().SetValidator(imageValidator);
        RuleFor(x => x.GenderId).GreaterThan(0);
        RuleFor(x => x.Age).GreaterThan(0);
    }
}