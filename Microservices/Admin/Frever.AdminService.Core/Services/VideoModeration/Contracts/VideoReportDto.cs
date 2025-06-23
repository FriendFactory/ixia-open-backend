using Frever.Shared.MainDb.Entities;

namespace Frever.AdminService.Core.Services.VideoModeration.Contracts;

public class VideoReportDto
{
    public VideoDto Video { get; set; }

    public VideoReport Report { get; set; }
}