using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Amazon.CloudFront;
using Amazon.Runtime;
using Amazon.Util;
using ThirdParty.BouncyCastle.OpenSsl;

namespace Common.Infrastructure.Aws.Crypto;

public static class FreverAmazonCloudFrontSigner
{
    private static RSAParameters _rsaParameters;
    private static RSA _rsa;

    private static SHA1 GetSha1Provider()
    {
        return SHA1.Create();
    }

    public static void Init(TextReader privateKey)
    {
        try
        {
            _rsaParameters = new PemReader(privateKey).ReadPrivatekey();
        }
        catch (Exception ex)
        {
            throw new AmazonClientException("Invalid RSA Private Key", ex);
        }

        _rsa = RSA.Create();
        _rsa.ImportParameters(_rsaParameters);
    }

    private static byte[] SignWithSha1Rsa(byte[] dataToSign, RSAParameters rsaParameters)
    {
        using var shA1Provider = GetSha1Provider();
        return GetRsapkcs1SignatureFromSha1(shA1Provider.ComputeHash(dataToSign), _rsa);
    }

    private static byte[] GetRsapkcs1SignatureFromSha1(byte[] hashedData, RSA providerRsa)
    {
        return providerRsa.SignHash(hashedData, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
    }

    public static string SignUrlCanned(string resourceUrlOrPath, string keyPairId, DateTime expiresOn)
    {
        var epochSecondsString = AWSSDKUtils.ConvertToUnixEpochSecondsString(expiresOn.ToUniversalTime());
        var str = MakeBytesUrlSafe(
            SignWithSha1Rsa(
                Encoding.UTF8.GetBytes(
                    "{\"Statement\":[{\"Resource\":\"" + resourceUrlOrPath + "\",\"Condition\":{\"DateLessThan\":{\"AWS:EpochTime\":" +
                    epochSecondsString + "}}}]}"
                ),
                _rsaParameters
            )
        );
        return resourceUrlOrPath + (resourceUrlOrPath.IndexOf('?') >= 0 ? "&" : "?") + "Expires=" + epochSecondsString + "&Signature=" +
               str + "&Key-Pair-Id=" + keyPairId;
    }

    private static string MakeBytesUrlSafe(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('=', '_').Replace('/', '~');
    }

    private static string MakeStringUrlSafe(string str)
    {
        return MakeBytesUrlSafe(Encoding.UTF8.GetBytes(str));
    }

    public static FreverAmazonCloudFrontCookiesForCustomPolicy GetCookiesForCustomPolicy(
        string resourceUrlOrPath,
        string keyPairId,
        DateTime expiresOn,
        DateTime activeFrom,
        string ipRange
    )
    {
        var cookiesForCustomPolicy = new FreverAmazonCloudFrontCookiesForCustomPolicy();
        var str = AmazonCloudFrontUrlSigner.BuildPolicyForSignedUrl(resourceUrlOrPath, expiresOn, ipRange, activeFrom);
        cookiesForCustomPolicy.Policy = new KeyValuePair<string, string>("CloudFront-Policy", MakeStringUrlSafe(str));
        cookiesForCustomPolicy.Signature = new KeyValuePair<string, string>(
            "CloudFront-Signature",
            MakeBytesUrlSafe(SignWithSha1Rsa(Encoding.UTF8.GetBytes(str), _rsaParameters))
        );
        cookiesForCustomPolicy.KeyPairId = new KeyValuePair<string, string>("CloudFront-Key-Pair-Id", keyPairId);
        return cookiesForCustomPolicy;
    }
}