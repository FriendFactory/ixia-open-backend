namespace Frever.AdminService.Core.Services.VideoModeration.Contracts;

public class VideoPatchRequest
{
    public int? StartListItem { get; set; }

    public bool? IsFeatured { get; set; }

    public bool? AllowComment { get; set; }

    public bool? AllowRemix { get; set; }
}