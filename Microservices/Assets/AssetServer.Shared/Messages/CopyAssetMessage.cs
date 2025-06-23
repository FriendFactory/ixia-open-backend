using System.Collections.Generic;

namespace AssetServer.Shared.Messages;

public class CopyAssetMessage
{
    public string FromKey { get; set; }

    public string ToKey { get; set; }

    public string Bucket { get; set; }

    public Dictionary<string, string> Tags { get; set; }
}