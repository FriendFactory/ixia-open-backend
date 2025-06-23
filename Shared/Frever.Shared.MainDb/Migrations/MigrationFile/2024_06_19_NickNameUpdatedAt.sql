begin;

alter table "Group" drop column if exists "Name";
alter table "Group" add column if not exists "NickNameUpdatedAt" timestamp with time zone;

commit;