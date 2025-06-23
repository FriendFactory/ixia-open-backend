-- Run this on Main db
-- Export results to CSV file
with us_usage as (select e."LevelId", mc."EventId", mc."UserSoundId", us."Name"
                  from "MusicController" mc
                           inner join "UserSound" us on mc."UserSoundId" = us."Id"
                           inner join "Event" e on mc."EventId" = e."Id"
                  where mc."UserSoundId" is not null
                  order by e."LevelId", e."LevelSequence"),
     us as (select *
                 , json_build_object('Id'
             , us_usage."UserSoundId"
             , 'Name'
             , us_usage."Name"
             , 'EventId'
             , us_usage."EventId") json
            from us_usage),
     vals as (select "LevelId" id, array_to_json(array_agg(us.json))::text val
              from us
              group by "LevelId")

select vals.id, vals.val
;
