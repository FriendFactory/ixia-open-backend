using System;

namespace Frever.Shared.MainDb.Entities;

public class AiCharacter
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public long GenderId { get; set; }
    public long ArtStyleId { get; set; }
    public string Ethnicity { get; set; }
    public string HairStyle { get; set; }
    public string HairColor { get; set; }
    public string FashionStyle { get; set; }
    public string Name { get; set; }
    public string Interests { get; set; }
    public string Description { get; set; }
    public int Age { get; set; }
    public DateTime? DeletedAt { get; set; }
}