using System;
using Newtonsoft.Json;

namespace Common.Infrastructure.Messaging;

public class VideoUnlikedMessage
{
    public long GroupId { get; set; }
    public long VideoId { get; set; }
    public DateTime Time { get; set; }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, SnsMessagingSettings.SerializerSettings);
    }
}