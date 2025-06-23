begin;

alter table "BodyAnimation"
    add column "AvailableForBackground" boolean default false not null;

commit;