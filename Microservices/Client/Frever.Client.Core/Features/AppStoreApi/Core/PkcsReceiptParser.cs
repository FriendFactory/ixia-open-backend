using System;
using System.Formats.Asn1;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Pkcs;
using System.Threading.Tasks;

namespace Frever.Client.Core.Features.AppStoreApi.Core;

public class PkcsReceiptParser(IHttpClientFactory httpClientFactory)
{
    private const string AppleRootCertUrl = "https://www.apple.com/certificateauthority/AppleRootCA-G3.cer";

    private HttpClient httpClient = httpClientFactory.CreateClient();

    public async Task ValidateReceipt(string receipt)
    {
        var receiptBytes = Convert.FromBase64String(receipt);
        var cert = await LoadAppleRootCert();

        var signedCms = new SignedCms();
        signedCms.Decode(receiptBytes);
        signedCms.CheckSignature(new X509Certificate2Collection(cert), true);
    }

    private async Task<X509Certificate2> LoadAppleRootCert()
    {
        var certBytes = await httpClient.GetByteArrayAsync(AppleRootCertUrl);
        return new X509Certificate2(certBytes);
    }
}