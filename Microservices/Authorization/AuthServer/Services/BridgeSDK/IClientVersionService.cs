namespace AuthServer.Services.BridgeSDK;

public interface IClientVersionService
{
    ClientVersions GetSupportedVersions();
}