begin;

alter table "Localization"
    add column if not exists "IsStartupItem" bool not null default false;

commit;