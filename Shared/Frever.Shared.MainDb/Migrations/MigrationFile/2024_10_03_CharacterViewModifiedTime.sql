begin;

alter table "Character"
    add column "ViewModifiedTime" timestamp with time zone;
update "Character"
set "ViewModifiedTime" = "ModifiedTime"
where "ViewModifiedTime" is null;
alter table "Character"
    alter column "ViewModifiedTime" set default CURRENT_TIMESTAMP;
alter table "Character"
    alter column "ViewModifiedTime" set not null;

commit;