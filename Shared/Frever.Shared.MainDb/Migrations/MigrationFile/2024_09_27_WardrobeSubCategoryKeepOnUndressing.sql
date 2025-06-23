begin;

alter table "WardrobeSubCategory"
    add column "KeepOnUndressing" boolean default false not null;

commit;