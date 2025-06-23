using System.Collections.Generic;

namespace Common.Infrastructure.ModerationProvider.TextModeration;

internal class Response
{
    public Input input { get; set; }
    public string language { get; set; }
    public List<Output> output { get; set; }
    public List<PiiEntity> pii_entities { get; set; }
    public List<CustomClass> custom_classes { get; set; }
}