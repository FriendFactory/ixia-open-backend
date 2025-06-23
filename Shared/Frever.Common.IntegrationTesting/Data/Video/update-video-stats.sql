with views as (select "VideoId",
                      count("VideoId") cnt
               from "Views"
               group by "VideoId"),
     likes as (select "VideoId",
                      count("VideoId") cnt
               from "Likes"
               group by "VideoId"),
     comments as (select "VideoId",
                         count("VideoId") cnt
                  from "Comments"
                  group by "VideoId"),
     shares as (select "VideoId",
                       count("VideoId") cnt
                from "Shares"
                group by "VideoId"),
     remixes as (select "RemixedFromVideoId" id,
                        count("Id")          cnt
                 from "Video"
                 where "RemixedFromVideoId" is not null
                 group by "RemixedFromVideoId"),
     video_stats as (select v."Id",
                            v."IsDeleted",
                            coalesce(views.cnt, 0)    views,
                            coalesce(likes.cnt, 0)    likes,
                            coalesce(comments.cnt, 0) comments,
                            coalesce(shares.cnt, 0)   shares,
                            coalesce(remixes.cnt, 0)  remixes
                     from "Video" v
                              left join
                          views on v."Id" = views."VideoId"
                              left join likes on views."VideoId" = likes."VideoId"
                              left join
                          comments on comments."VideoId" = v."Id"
                              left join
                          shares on v."Id" = shares."VideoId"
                              left join
                          remixes on v."Id" = remixes.id),
     upd as (
         update stats.video_kpi
             set
                 likes = s.likes,
                 views = s.views,
                 comments = s.comments,
                 shares = s.shares,
                 remixes = s.remixes,
                 deleted = s."IsDeleted"
             from video_stats s
             where s."Id" = stats.video_kpi.video_id)
insert
into stats.video_kpi (video_id, likes, views, comments, shares, remixes, battles_won, battles_lost, deleted)
select s."Id",
       s.likes,
       s.views,
       s.comments,
       s.shares,
       s.remixes,
       0,
       0,
       s."IsDeleted"
from video_stats s
where not exists (select 1 from stats.video_kpi k where k.video_id = s."Id");