begin;

create table if not exists "ContentRow"
(
    "Id"            bigint generated always as identity primary key,
    "Title"         text    not null,
    "SortOrder"     integer not null default 0,
    "TestGroup"     text,
    "ContentType"   text    not null,
    "ContentIds"    bigint[],
    "ContentQuery"  text,
    "IsEnabled"     bool    not null default false
);

create index idx_external_song_usage_count
    on "ExternalSong" ("UsageCount")
    where not "IsDeleted" and not "IsManuallyDeleted" and "NotClearedSince" is null;

commit;