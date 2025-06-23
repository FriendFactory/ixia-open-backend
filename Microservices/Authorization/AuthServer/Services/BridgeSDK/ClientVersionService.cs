using Microsoft.Extensions.Configuration;

namespace AuthServer.Services.BridgeSDK;

internal class ClientVersionService : IClientVersionService
{
    private const string BRIDGE_MIN_VERSION = "ClientVersion:BridgeMinVersion";
    private const string BRIDGE_MAX_VERSION = "ClientVersion:BridgeMaxVersion";

    private const string FREVER_MIN_BUILD = "ClientVersion:FreverMinBuild";
    private const string FREVER_MAX_BUILD = "ClientVersion:FreverMaxBuild";

    private const string FREVER_MIN_VERSION = "ClientVersion:FreverMinVersion";
    private const string FREVER_MAX_VERSION = "ClientVersion:FreverMaxVersion";

    private readonly ClientVersions _versions;

    public ClientVersionService(IConfiguration configuration)
    {
        _versions = new ClientVersions
                    {
                        BridgeMinVersion = configuration.GetValue<string>(BRIDGE_MIN_VERSION),
                        BridgeMaxVersion = configuration.GetValue<string>(BRIDGE_MAX_VERSION),
                        FreverMinVersion = configuration.GetValue<string>(FREVER_MIN_VERSION),
                        FreverMaxVersion = configuration.GetValue<string>(FREVER_MAX_VERSION)
                    };

        var minBuildString = configuration.GetValue<string>(FREVER_MIN_BUILD);
        var maxBuildString = configuration.GetValue<string>(FREVER_MAX_BUILD);

        _versions.FreverMinBuild = int.TryParse(minBuildString, out var minBuild) ? minBuild : 0;
        _versions.FreverMaxBuild = int.TryParse(maxBuildString, out var maxBuild) ? maxBuild : int.MaxValue;
    }

    public ClientVersions GetSupportedVersions()
    {
        return _versions;
    }
}