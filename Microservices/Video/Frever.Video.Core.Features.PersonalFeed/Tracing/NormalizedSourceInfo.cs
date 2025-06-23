using Frever.Video.Contract;

namespace Frever.Video.Core.Features.PersonalFeed.Tracing;

public class NormalizedSourceInfo
{
    public string SourceName { get; set; }

    public VideoRef[] Videos { get; set; }

    public int Count { get; set; }

    public double InitialWeight { get; set; }

    public double NormalizedWeight { get; set; }
}