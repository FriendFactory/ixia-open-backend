begin;

create table if not exists tmp_user_sound_info
(
    level_id   bigint not null,
    user_sound text   not null
);

-- If this is empty Copy data from CSV manually using Data Grip Import data from file feature
select * from tmp_user_sound_info;


update "Video"
set "UserSoundInfo" = v.user_sound
from tmp_user_sound_info v
where v.level_id = "Video"."LevelId" and "Video"."UserSoundInfo" = '[]';

select count(1)
from "Video"
where "UserSoundInfo" <> '[]';

drop table tmp_user_sound_info;

commit ;