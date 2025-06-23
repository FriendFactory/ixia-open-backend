using System;
using Newtonsoft.Json;

namespace Common.Infrastructure.Messaging;

public class SnsMessage
{
    public string Subject { get; set; }
    public string MessageId { get; set; }
    public string Payload { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, SnsMessagingSettings.SerializerSettings);
    }
}