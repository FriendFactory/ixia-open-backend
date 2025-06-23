using System;
using System.Linq;
using System.Threading.Tasks;
using Frever.Protobuf;

namespace Frever.Shared.AssetStore.OfferKeyCodec;

public class SimpleInAppProductOfferKeyCodec : IInAppProductOfferKeyCodec
{
    public Task<string> Encode(long groupId, InAppProductOfferPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        payload.InAppProductDetailIds = payload.InAppProductDetailIds.OrderBy(e => e).ToArray();
        var data = ProtobufConvert.SerializeObject(payload);

        return Task.FromResult(Convert.ToBase64String(data));
    }

    public Task<InAppProductOfferPayload> DecodeAndValidate(long groupId, string offerKey)
    {
        if (string.IsNullOrWhiteSpace(offerKey))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(offerKey));
        var bytes = Convert.FromBase64String(offerKey);
        var payload = ProtobufConvert.DeserializeObject<InAppProductOfferPayload>(bytes);

        return Task.FromResult(payload);
    }

    public Task<InAppProductOfferPayload> DecodeUnsafe(string offerKey)
    {
        return DecodeAndValidate(-1, offerKey);
    }
}