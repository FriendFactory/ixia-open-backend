using System;
using Common.Models.Files;

namespace Frever.AdminService.Core.Services.Social.Contracts;

public class ProfileDto : IFileMetadataOwner
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long MainGroupId { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string GoogleId { get; set; }
    public string AppleId { get; set; }
    public string NickName { get; set; }
    public string Bio { get; set; }
    public long DefaultLanguageId { get; set; }
    public long? TaxationCountryId { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? DeletedAt { get; set; }
    public ProfileKpiDto Kpi { get; set; }
    public FileMetadata[] Files { get; set; }
}