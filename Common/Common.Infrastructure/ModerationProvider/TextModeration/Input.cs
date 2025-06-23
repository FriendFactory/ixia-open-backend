using System;

namespace Common.Infrastructure.ModerationProvider.TextModeration;

internal class Input
{
    public string hash { get; set; }
    public string inference_client_version { get; set; }
    public string model { get; set; }
    public string model_type { get; set; }
    public int model_version { get; set; }
    public string text { get; set; }
    public string id { get; set; }
    public DateTime created_on { get; set; }
    public int user_id { get; set; }
    public int project_id { get; set; }
    public double charge { get; set; }
}