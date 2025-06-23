using System;

#pragma warning disable CS8618

namespace Frever.Shared.MainDb.Entities;

public class VideoReport
{
    public long Id { get; set; }
    public long VideoId { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? ClosedTime { get; set; }
    public string Message { get; set; }
    public long ReasonId { get; set; }
    public long? ReporterGroupId { get; set; }
    public long? AssignedToUserId { get; set; }
    public long? ClosedByUserId { get; set; }
    public string ModerationNotes { get; set; }
    public bool HideVideo { get; set; }
}