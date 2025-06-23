using System;
using System.Collections.Generic;
using Common.Models.Files;

namespace Frever.AdminService.Core.Services.InAppPurchases.Contracts;

public class InAppProductDto
{
    public long Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string AppStoreProductRef { get; set; }
    public string PlayMarketProductRef { get; set; }
    public bool IsActive { get; set; }
    public bool IsSeasonPass { get; set; }
    public long? InAppProductPriceTierId { get; set; }
    public long SortOrder { get; set; }
    public List<FileInfo> Files { get; set; }
    public DateTime? PublicationDate { get; set; }
    public DateTime? DepublicationDate { get; set; }
    public InAppProductDetailsDto[] ProductDetails { get; set; }
}