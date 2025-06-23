using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.VideoModeration.Contracts;

public class VideoShortDto
{
    public long Id { get; set; }

    public long? LevelId { get; set; }

    public long? SchoolTaskId { get; set; }

    public long? RemixedFromVideoId { get; set; }

    public bool IsDeleted { get; set; }

    public VideoAccess Access { get; set; }
}