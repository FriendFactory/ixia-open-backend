namespace Frever.AdminService.Core.Services.HashtagModeration.Contracts;

public class GetHashtagsRequest
{
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 20;
    public string Name { get; set; }
    public string OrderByColumnName { get; set; }
    public bool? Descending { get; set; }
}