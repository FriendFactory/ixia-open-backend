namespace Frever.AdminService.Core.Services.GeoClusters;

public class GeoClusterDto
{
    public long Id { get; set; }
    public int Priority { get; set; }
    public string Title { get; set; }
    public bool IsActive { get; set; }
    public int RecommendationVideosPool { get; set; }
    public int RecommendationNumOfDaysLookback { get; set; }

    public string[] IncludeVideoFromCountry { get; set; }
    public string[] ExcludeVideoFromCountry { get; set; }
    public string[] IncludeVideoWithLanguage { get; set; }
    public string[] ExcludeVideoWithLanguage { get; set; }
    public string[] ShowToUserFromCountry { get; set; }
    public string[] HideForUserFromCountry { get; set; }
    public string[] ShowForUserWithLanguage { get; set; }
    public string[] HideForUserWithLanguage { get; set; }
}