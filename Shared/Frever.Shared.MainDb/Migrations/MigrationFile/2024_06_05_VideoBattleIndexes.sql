begin;

create index idx_battle_pair_battle_group_id_left_video_id_right_video_id
    on "BattlePair" ("BattleGroupId", "LeftVideoId", "RightVideoId");

create index idx_battle_pair_left_video_id_right_video_id
    on "BattlePair" ("LeftVideoId", "RightVideoId");

create index idx_battle_pair_left_video_id
    on "BattlePair" ("LeftVideoId");

create index idx_battle_pair_right_video_id
    on "BattlePair" ("RightVideoId");

create index idx_battle_group_id_school_task_id
    ON "BattleGroup" ("Id", "SchoolTaskId");

create index idx_battle_group_school_task_id
    on "BattleGroup" ("SchoolTaskId");

create index idx_battle_group_end_time
    on "BattleGroup" ("EndTime");

commit;