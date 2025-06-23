insert into "GeoCluster"("Priority", "Title", "IncludeVideoFromCountry",
                         "IncludeVideoWithLanguage", "ShowToUserFromCountry",
                         "ShowForUserWithLanguage")
values (:priority,
        :title,
        :includeVideosFromCountry,
        :includeVideosWithLanguage,
        :showToUserFromCountry,
        :showToUserWithLanguage)
returning *;
