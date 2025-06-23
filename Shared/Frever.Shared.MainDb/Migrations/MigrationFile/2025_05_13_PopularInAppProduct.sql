begin;

alter table "InAppProduct" add column "IsPopular" bool not null default false;

commit;