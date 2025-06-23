begin;

create table "AiArtStyle"
(
    "Id"        bigint  not null generated always as identity primary key,
    "Name"      text    not null,
    "Text"      text    not null,
    "IsEnabled" bool    not null default false,
    "SortOrder" integer not null default 0,
    "Files"     json    null
);

alter table "AiCharacter"
    add column if not exists "ArtStyleId" bigint not null references "AiArtStyle" ("Id") default 1;

commit;