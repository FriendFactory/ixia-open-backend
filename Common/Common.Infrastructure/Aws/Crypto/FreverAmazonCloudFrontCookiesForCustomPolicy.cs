using System.Collections.Generic;

namespace Common.Infrastructure.Aws.Crypto;

public class FreverAmazonCloudFrontCookiesForCustomPolicy
{
    public KeyValuePair<string, string> Policy { get; set; }
    public KeyValuePair<string, string> KeyPairId { get; set; }
    public KeyValuePair<string, string> Signature { get; set; }
}