using System.Collections.Generic;

namespace Common.Infrastructure.ModerationProvider.VisualModeration;

internal class Response
{
    public Input input { get; set; }
    public List<Output> output { get; set; }
}