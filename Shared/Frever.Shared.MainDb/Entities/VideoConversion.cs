using System;

namespace Frever.Shared.MainDb.Entities;

[Flags]
public enum VideoConversion
{
    Started = 0,
    VideoConverted = 1,
    ThumbnailConverted = 2,
    Completed = VideoConverted | ThumbnailConverted
}