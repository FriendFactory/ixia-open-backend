using System.Collections.Generic;

namespace Frever.Video.Contract.Messages;

public class CreateConversionJobMessage
{
    public long VideoId { get; set; }

    public string RoleArn { get; set; }

    public string JobTemplateName { get; set; }

    public string Queue { get; set; }

    public Dictionary<string, string> UserMetadata { get; set; }

    public string SourceBucketPath { get; set; }

    public string DestinationBucketPath { get; set; }

    public bool HasLandscapeOrientation { get; set; }
}