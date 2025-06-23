namespace Frever.Client.Shared.AI.ComfyUi.Contract;

public class ComfyUiResponse
{
    public static readonly ComfyUiResponse Success = new() {IsSuccess = true, Message = ""};

    public bool IsSuccess { get; private init; }
    public string Message { get; private init; }
    public string ErrorCode { get; private set; }
    public long AiContentId { get; set; }

    public static ComfyUiResponse Failed(string message, string errorCode = "")
    {
        return new ComfyUiResponse {IsSuccess = false, Message = message, ErrorCode = errorCode};
    }
}