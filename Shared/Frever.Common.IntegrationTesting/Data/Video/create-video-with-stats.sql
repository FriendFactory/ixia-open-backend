with video as (
    insert into "Video" ("LevelId", "GroupId", "ToplistPosition", "IsDeleted", "Version", "IsRemixable",
                         "TemplateIds", "Description", "Access", "Language", "Country", "SongInfo",
                         "PublishTypeId", "AllowRemix", "AllowComment", "SchoolTaskId", "CreatedTime",
                         "AvailableForRating", "RatingCompleted",
                         "Location", "VerticalCategoryId", "Size", "ResolutionWidth", "ResolutionHeight", "Duration",
                         "FrameRate", "PlatformId", "Watermark", "Public", "SongName", "ArtistName", "CharactersCount",
                         "AiContentId")
        values (:levelId,
                :groupId,
                :toplistPosition,
                :isDeleted,
                :version,
                :isRemixable,
                :templateIds,
                :description,
                :access,
                :language,
                :country,
                :songs,
                1,
                :allowRemix,
                :allowComment,
                :schoolTaskId,
                :createdTime,
                :availableForRating,
                :ratingCompleted,
                ST_Point(22.3, 22.3),
                1, 234234, 1920, 1080, 4, 29, 2, false, true, null, null, 1, :aiContentId)
        returning *),
     updated_video as (
         update stats.video_kpi
             set
                 likes = :likes,
                 views = :views,
                 comments = :comments,
                 shares = :shares,
                 remixes = :remixes,
                 battles_won = :battlesWon,
                 battles_lost = :battlesLost,
                 deleted = video."IsDeleted"
             from video
             where
                 stats.video_kpi.video_id = video."Id"),
     inserted as (
         insert into stats.video_kpi (video_id, likes, views, comments, shares, remixes, battles_won, battles_lost,
                                      deleted)
             select "Id",
                    :likes,
                    :views,
                    :comments,
                    :shares,
                    :remixes,
                    :battlesWon,
                    :battlesLost,
                    "IsDeleted"
             from video)
select *
from video;