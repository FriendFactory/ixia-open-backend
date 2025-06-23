begin;

alter table "Gender" add column "CanCreateCharacter" boolean default true not null;

commit;