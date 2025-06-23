begin;

alter table "Vfx"
    add column "FollowRotation" boolean default false not null;

update "Vfx"
set "FollowRotation" = true
where "VfxCategoryId" in (5, 7, 8, 9);

commit;