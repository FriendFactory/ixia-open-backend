namespace Frever.Shared.MainDb.Entities
{
    public class GeoCluster
    {
        public long Id { get; set; }
        public int Priority { get; set; }
        public string Title { get; set; }
        public bool IsActive { get; set; }
        public string[] IncludeVideoFromCountry {get;set;}
        public string[] ExcludeVideoFromCountry {get;set;}
        public string[] IncludeVideoWithLanguage {get;set;}
        public string[] ExcludeVideoWithLanguage {get;set;}
        public string[] ShowToUserFromCountry {get;set;}
        public string[] HideForUserFromCountry {get;set;}
        public string[] ShowForUserWithLanguage {get;set;}
        public string[] HideForUserWithLanguage {get;set;}

        /// <summary>
        /// Gets or sets number of videos to fetch to select recommendation from
        /// </summary>
        public int RecommendationVideosPool { get; set; }

        /// <summary>
        /// Gets or sets number of days to look back to select videos to select recommendation from
        /// </summary>
        public int RecommendationNumOfDaysLookback { get; set; }

        public override string ToString()
        {
            return Title;
        }
    }
}