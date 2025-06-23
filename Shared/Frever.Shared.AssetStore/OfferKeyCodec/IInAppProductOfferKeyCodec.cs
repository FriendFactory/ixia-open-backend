using System.Threading.Tasks;

namespace Frever.Shared.AssetStore.OfferKeyCodec;

public interface IInAppProductOfferKeyCodec
{
    Task<string> Encode(long groupId, InAppProductOfferPayload payload);

    /// <summary>
    ///     Decode offer key and validate if it fresh and issued for the specified group.
    /// </summary>
    Task<InAppProductOfferPayload> DecodeAndValidate(long groupId, string offerKey);

    Task<InAppProductOfferPayload> DecodeUnsafe(string offerKey);
}