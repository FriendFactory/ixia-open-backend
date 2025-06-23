using Common.Models.Files;

namespace Frever.AdminService.Core.Services.Social.Contracts;

public class ProfileShortDto : IFileMetadataOwner
{
    public long Id { get; set; }
    public long MainGroupId { get; set; }

    public string NickName { get; set; }

    public ProfileKpiDto Kpi { get; set; }

    public FileMetadata[] Files { get; set; }
}