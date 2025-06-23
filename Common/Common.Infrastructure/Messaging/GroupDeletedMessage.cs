using Newtonsoft.Json;

namespace Common.Infrastructure.Messaging;

public class GroupDeletedMessage
{
    public long GroupId { get; set; }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, SnsMessagingSettings.SerializerSettings);
    }
}