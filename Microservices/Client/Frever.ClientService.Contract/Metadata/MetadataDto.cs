using Frever.Protobuf;

namespace Frever.ClientService.Contract.Metadata;

public class MetadataDto
{
    public GenreDto[] Genres { get; set; }
    public GenderDto[] Genders { get; set; }
    public WardrobeModeDto[] WardrobeModes { get; set; }
    public AiWorkflowMetadataInfo[] Workflows { get; set; }
    public int AllowedCharactersCount { get; set; }
    public CharacterCreationOptions CharacterCreationOptions { get; set; }
    [ProtoNewField(1)] public MakeUpCategoryDto[] MakeUpCategories { get; set; }
}

public class CharacterCreationOptions
{
    public string[] Ethnicities { get; set; }
    public string[] HairColors { get; set; }
    public string[] HairStyles { get; set; }
}