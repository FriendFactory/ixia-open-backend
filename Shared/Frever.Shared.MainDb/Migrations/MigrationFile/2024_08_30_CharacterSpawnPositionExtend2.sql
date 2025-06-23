begin;

alter table "CharacterSpawnPosition" drop column "KeepPoseAnimationCategoryIds";
drop trigger check_spawn_position_pose_animation_categories on "CharacterSpawnPosition";
drop function check_spawn_position_pose_animation_categories;

alter table "CharacterSpawnPosition" add column if not exists "KeepAnimationCategoryIds" bigint[];

update "CharacterSpawnPosition"
set "KeepAnimationCategoryIds" = COALESCE("KeepAnimationCategoryIds", '{}') ||
                                 (select bac."Id" from "BodyAnimationCategory" bac where bac."Name" = 'Poses')
where "MovementTypeId" = 3;

create or replace function check_spawn_position_animation_categories() returns trigger as $$
begin
    if new."KeepAnimationCategoryIds" is not null and array_length(new."KeepAnimationCategoryIds", 1) > 0 then
        perform 1
        from unnest(new."KeepAnimationCategoryIds") as array_id
        where not exists (select 1 from "BodyAnimationCategory" where "Id" = array_id);

        if found then
            raise exception 'Invalid reference to BodyAnimationCategory in KeepAnimationCategoryIds of CharacterSpawnPosition';
        end if;
    end if;
    return new;
end;
$$ language plpgsql;

create or replace trigger check_spawn_position_animation_categories
    before insert or update on "CharacterSpawnPosition"
    for each row execute function check_spawn_position_animation_categories();

commit;