namespace Frever.Shared.AssetStore.OfferKeyCodec;

public class InAppProductOfferPayload
{
    public long InAppProductId { get; set; }

    public long[] InAppProductDetailIds { get; set; } = [];
}