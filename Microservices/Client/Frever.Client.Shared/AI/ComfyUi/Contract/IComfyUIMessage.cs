namespace Frever.Client.Shared.AI.ComfyUi.Contract;

public interface IComfyUiMessage
{
    public void Enrich(string env, string s3Bucket, long groupId);
    public void Validate();
    public string ToResultKey(string workflow);
    public string ToJson();
}