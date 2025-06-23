using System.Collections.Generic;
using Common.Models.Files;
using Frever.AdminService.Core.Utils;

namespace Frever.AdminService.Core.Services.InAppPurchases.Contracts;

public class InAppProductDetailsDto
{
    public long Id { get; set; }
    public long InAppProductId { get; set; }
    public long? AssetId { get; set; }
    public ShortAssetTypeDto? AssetType { get; set; }
    public int? HardCurrency { get; set; }
    public int? SoftCurrency { get; set; }
    public long SortOrder { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public int UniqueOfferGroup { get; set; }
    public List<FileInfo> Files { get; set; }
}