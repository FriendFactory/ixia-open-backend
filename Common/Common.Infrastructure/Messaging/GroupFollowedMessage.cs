using System;
using Newtonsoft.Json;

namespace Common.Infrastructure.Messaging;

public class GroupFollowedMessage
{
    public long FollowingId { get; set; }
    public long FollowerId { get; set; }
    public bool IsMutual { get; set; }
    public DateTime Time { get; set; }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, SnsMessagingSettings.SerializerSettings);
    }
}