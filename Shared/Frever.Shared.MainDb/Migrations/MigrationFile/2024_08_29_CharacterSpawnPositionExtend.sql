begin;

alter table "CharacterSpawnPosition"
    add column if not exists "SecondaryMovementTypeIds"         bigint[],
    add column if not exists "KeepPoseAnimationCategoryIds"     bigint[];

update "CharacterSpawnPosition"
set "SecondaryMovementTypeIds" = COALESCE("SecondaryMovementTypeIds", '{}') || 4
where "MovementTypeId" = 3;

create or replace function check_spawn_position_secondary_movement_types() returns trigger as $$
begin
    if new."SecondaryMovementTypeIds" is not null and array_length(new."SecondaryMovementTypeIds", 1) > 0 then
        perform 1
        from unnest(new."SecondaryMovementTypeIds") as array_id
        where not exists (select 1 from "MovementType" where "Id" = array_id);

        if found then
            raise exception 'Invalid reference to MovementType in SecondaryMovementTypeIds of CharacterSpawnPosition';
        end if;
    end if;
    return new;
end;
$$ language plpgsql;

create or replace trigger check_spawn_position_secondary_movement_types
    before insert or update on "CharacterSpawnPosition"
    for each row execute function check_spawn_position_secondary_movement_types();

create or replace function check_spawn_position_pose_animation_categories() returns trigger as $$
begin
    if new."KeepPoseAnimationCategoryIds" is not null and array_length(new."KeepPoseAnimationCategoryIds", 1) > 0 then
        perform 1
        from unnest(new."KeepPoseAnimationCategoryIds") as array_id
        where not exists (select 1 from "BodyAnimationCategory" where "Id" = array_id);

        if found then
            raise exception 'Invalid reference to MovementType in SecondaryMovementTypeIds of CharacterSpawnPosition';
        end if;
    end if;
    return new;
end;
$$ language plpgsql;

create or replace trigger check_spawn_position_pose_animation_categories
    before insert or update on "CharacterSpawnPosition"
    for each row execute function check_spawn_position_pose_animation_categories();

commit;