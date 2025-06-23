begin;

alter table "AiArtStyle"
    add column "GenderId" bigint not null default 1 references "Gender";

commit;