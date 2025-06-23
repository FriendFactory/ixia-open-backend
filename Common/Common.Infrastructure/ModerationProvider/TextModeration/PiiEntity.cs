namespace Common.Infrastructure.ModerationProvider.TextModeration;

internal class PiiEntity
{
    public string value { get; set; }
    public int start_index { get; set; }
    public int end_index { get; set; }
    public string type { get; set; }
}