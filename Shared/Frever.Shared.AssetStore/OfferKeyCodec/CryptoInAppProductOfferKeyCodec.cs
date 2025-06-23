using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Frever.Shared.AssetStore.OfferKeyCodec;

public class CryptoInAppProductOfferKeyCodec(AssetStoreOptions options) : IInAppProductOfferKeyCodec
{
    private readonly AssetStoreOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public Task<string> Encode(long groupId, InAppProductOfferPayload payload)
    {
        var internalPayload = ToInternalPayload(groupId, payload);
        var payloadSerialized = JsonConvert.SerializeObject(internalPayload);
        var data = Encoding.UTF8.GetBytes(payloadSerialized); // ProtobufConvert2.SerializeObject(payload);

        using var aesAlg = Aes.Create();
        var key = AesKey(aesAlg.KeySize);

        using var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV);

        using var encryptedData = new MemoryStream();
        using var encryptorStream = new CryptoStream(encryptedData, encryptor, CryptoStreamMode.Write);

        encryptorStream.Write(data, 0, data.Length);

        var encrypted = encryptedData.ToArray();


        return Task.FromResult(Convert.ToBase64String(encrypted));
    }

    public Task<InAppProductOfferPayload> DecodeAndValidate(long groupId, string offerKey)
    {
        using var aesAlg = Aes.Create();
        var key = AesKey(aesAlg.KeySize);
        using var encryptor = aesAlg.CreateDecryptor(key, aesAlg.IV);

        using var encryptedData = new MemoryStream(Encoding.UTF8.GetBytes(offerKey));
        using var decryptorStream = new CryptoStream(encryptedData, encryptor, CryptoStreamMode.Read);

        using var streamReader = new StreamReader(decryptorStream);
        var decryptedData = streamReader.ReadToEnd();


        var payload = JsonConvert.DeserializeObject<InAppProductOfferPayload>(decryptedData);

        return Task.FromResult(payload);
    }

    public Task<InAppProductOfferPayload> DecodeUnsafe(string offerKey)
    {
        throw new NotImplementedException();
    }

    private byte[] AesKey(int keySize)
    {
        var key = Encoding.UTF8.GetBytes(_options.OfferKeySecret);
        var keyPadded = new byte[keySize];

        var span = key[..Math.Min(key.Length, keyPadded.Length)];
        span.CopyTo(keyPadded, 0);

        return keyPadded;
    }

    private PayloadInternal ToInternalPayload(long groupId, InAppProductOfferPayload payload)
    {
        return new PayloadInternal
               {
                   DayOfYear = DateTime.UtcNow.DayOfYear,
                   GroupId = groupId,
                   Seed = new Random().Next(),
                   InAppProductId = payload.InAppProductId,
                   InAppProductDetailIds = payload.InAppProductDetailIds
               };
    }

    public class PayloadInternal : InAppProductOfferPayload
    {
        public int Seed { get; set; }

        public int DayOfYear { get; set; }

        public long GroupId { get; set; }
    }
}