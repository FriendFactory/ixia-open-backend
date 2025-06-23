using System;

namespace Common.Infrastructure.ModerationProvider.VisualModeration;

internal class Input
{
    public string id { get; set; }
    public double charge { get; set; }
    public string model { get; set; }
    public int model_version { get; set; }
    public string model_type { get; set; }
    public DateTime created_on { get; set; }
    public Media media { get; set; }
    public int user_id { get; set; }
    public int project_id { get; set; }
    public int config_version { get; set; }
    public string config_tag { get; set; }
}