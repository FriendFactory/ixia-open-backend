with song_info as (select e."LevelId",
                          mc."EventId",
                          coalesce(s."Id", es."ExternalTrackId") "SongId",
                          coalesce(a."Name", es."ArtistName")    "Artist",
                          coalesce(s."Name", es."SongName")      "Title",
                          es."ExternalTrackId" is not null       "IsExternal",
                          es."Isrc"
                   from "MusicController" mc
                            inner join "Event" e on mc."EventId" = e."Id"
                            left join "ExternalSong" es on mc."ExternalTrackId" = es."ExternalTrackId"
                            left join "Song" s on mc."SongId" = s."Id"
                            left join "Artist" a on s."ArtistId" = a."Id"
                   where mc."ExternalTrackId" is not null
                      or mc."SongId" is not null),
     songs_json as (select distinct "LevelId",
                           json_build_object(
                                   'Id'
                               , song_info."SongId"
                               , 'Artist'
                               , song_info."Artist"
                               , 'Title'
                               , song_info."Title"
                               , 'IsExternal'
                               , song_info."IsExternal"
                               , 'Isrc'
                               , song_info."Isrc"
                               )::text json

                    from song_info),
     vals as (select "LevelId" id, array_to_json(array_agg(songs_json.json::json))::text val
              from songs_json
              group by "LevelId")
select *
from vals
;