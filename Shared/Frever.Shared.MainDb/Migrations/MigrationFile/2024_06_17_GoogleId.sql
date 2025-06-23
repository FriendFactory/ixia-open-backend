begin;

alter table "User" add column "GoogleId" text unique;

commit;