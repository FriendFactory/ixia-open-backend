namespace Frever.Client.Core.Features.AI.UserGeneratedContent.Characters.Contracts;

public class AiCharacterDto
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public AiCharacterImageDto Image { get; set; }
    public long GenderId { get; set; }
    public int Age { get; set; }
    public string ArtStyle { get; set; }
    public string Ethnicity { get; set; }
    public string HairStyle { get; set; }
    public string HairColor { get; set; }
    public string FashionStyle { get; set; }
    public string Interests { get; set; }
}