namespace Frever.Client.Core.Features.Sounds;

public interface IAssetServerSettings
{
    int NewAssetDays { get; }
}

public sealed class AssetServerSettings : IAssetServerSettings
{
    public int NewAssetDays { get; set; }
}