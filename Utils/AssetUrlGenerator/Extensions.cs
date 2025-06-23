using Microsoft.Extensions.DependencyInjection;

namespace AssetStoragePathProviding;

public static class Extensions
{
    public static void AddAssetBucketPathService(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IAssetFilesConfigs, AssetFilesConfigs>();
        serviceCollection.AddSingleton<IFileBucketPathService, FileBucketPathService>();
    }
}