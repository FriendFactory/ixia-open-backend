namespace Frever.Client.Shared.AI.ComfyUi.Contract;

public record ComfyUiResult(
    string PartialName,
    string Bucket,
    string MainKey,
    string CoverKey,
    string ThumbnailKey,
    string MaskKey
);