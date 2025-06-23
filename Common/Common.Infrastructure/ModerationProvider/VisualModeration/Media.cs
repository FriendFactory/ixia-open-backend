namespace Common.Infrastructure.ModerationProvider.VisualModeration;

internal class Media
{
    public object url { get; set; }
    public object filename { get; set; }
    public string type { get; set; }
    public string mime_type { get; set; }
    public string mimetype { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public int num_frames { get; set; }
    public double duration { get; set; }
}