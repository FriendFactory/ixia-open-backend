using Newtonsoft.Json;

namespace Common.Infrastructure.Messaging;

public class SnsMessagingSettings
{
    public static readonly JsonSerializerSettings SerializerSettings = new() {NullValueHandling = NullValueHandling.Ignore};

    public string VideoTemplateMappingTopicArn { get; set; }
    public string GroupChangedTopicArn { get; set; }
    public string TemplateUpdatedTopicArn { get; set; }
    public string GroupDeletedTopicArn { get; set; }
    public string VideoUnlikedTopicArn { get; set; }
    public string GroupUnfollowedTopicArn { get; set; }
    public string GroupFollowedTopicArn { get; set; }
    public string OutfitChangedTopicArn { get; set; }
}