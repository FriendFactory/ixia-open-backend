begin;

alter table "Character"
    add column "AvailableForBakingSince" timestamp with time zone default CURRENT_TIMESTAMP not null;

update "Character"
set "AvailableForBakingSince" = "ViewModifiedTime"
where "AvailableForBakingSince" != now()

commit;