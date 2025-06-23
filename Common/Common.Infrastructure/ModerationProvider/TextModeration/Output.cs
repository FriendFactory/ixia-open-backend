using System.Collections.Generic;

namespace Common.Infrastructure.ModerationProvider.TextModeration;

internal class Output
{
    public int time { get; set; }
    public int start_char_index { get; set; }
    public int end_char_index { get; set; }
    public List<Class> classes { get; set; }
}