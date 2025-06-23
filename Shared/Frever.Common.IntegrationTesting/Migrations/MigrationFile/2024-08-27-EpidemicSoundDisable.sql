begin;

-- Run this on test db only
-- Had run manually for other environments

-- 33 - Epidemic Sound LabelId
-- 19 - Discontinued ReadinessId

with epidemic_songs as (select *
                        from "Song"
                        where "LabelId" = 33),

     events_with_es as (select e.*
                        from "Event" e
                                 inner join
                             "MusicController" mc on e."Id" = mc."EventId"
                        where mc."SongId" in (select "Id"
                                              from epidemic_songs)),
     levels_with_es as (select *
                        from "Level" l
                        where exists (select 1 from events_with_es ev where ev."LevelId" = l."Id")),
     templates_with_es as (select t.*
                           from "Template" t
                           where exists (select 1
                                         from events_with_es ev
                                         where ev."Id" = t."EventId")),
     upd_video_with_levels as (
         update "Video"
             set "IsRemixable" = false
                 , "AllowRemix" = false
             where "LevelId" in (select "Id" from levels_with_es)),
     upd_songs as (update "Song"
         set "ReadinessId" = 19
         where "Id" in (select "Id" from epidemic_songs)),
     upd_templates as (
         update "Template"
             set "IsDeleted" = true
             where "Id" in (select "Id" from templates_with_es)
             returning *)
select *
from upd_templates;

commit;