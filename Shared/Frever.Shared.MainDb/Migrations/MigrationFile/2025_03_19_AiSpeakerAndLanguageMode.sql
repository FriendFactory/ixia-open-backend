begin;

create table "AiSpeakerMode"
(
    "Id"        bigint  not null primary key,
    "Name"      text    not null,
    "IsDefault" bool    not null default false,
    "SortOrder" integer not null default 0,
    "Files"     json    null
);

create table "AiLanguageMode"
(
    "Id"        bigint  not null primary key,
    "Name"      text    not null,
    "IsDefault" bool    not null default false,
    "SortOrder" integer not null default 0,
    "Files"     json    null
);

commit;