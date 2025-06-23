namespace Frever.AdminService.Core.Services.MusicModeration.Contracts;

public interface ISoundMetadata
{
    long Id { get; set; }
}

public class ArtistDto : ISoundMetadata
{
    public long Id { get; set; }
    public string Name { get; set; }
}

public class AlbumDto : ISoundMetadata
{
    public long Id { get; set; }
    public string Name { get; set; }
    public long? ArtistId { get; set; }
}

public class BrandDto : ISoundMetadata
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public string Name { get; set; }
}

public class GenreDto : ISoundMetadata
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int SortOrder { get; set; }
    public long LabelId { get; set; }
    public string[] Countries { get; set; }
}

public class LabelDto : ISoundMetadata
{
    public long Id { get; set; }
    public string Name { get; set; }
}

public class MoodDto : ISoundMetadata
{
    public long Id { get; set; }
    public string Name { get; set; }
}