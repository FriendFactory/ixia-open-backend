namespace AssetStoragePathProviding
{
    public interface IVideoNameSource
    {
        long Id { get; }

        long GroupId { get; }

        string Version { get; }
    }
}