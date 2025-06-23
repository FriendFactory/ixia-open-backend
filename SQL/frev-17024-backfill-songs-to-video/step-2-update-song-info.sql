begin;

create table if not exists tmp_song_info
(
    level_id  bigint not null,
    song_info text   not null
);

-- If this is empty Copy data from CSV manually using Data Grip Import data from file feature
select *
from tmp_song_info;


update "Video"
set "SongInfo" = info.song_info
from tmp_song_info info
where info.level_id = "Video"."LevelId";

drop table tmp_song_info;

commit ;