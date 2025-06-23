begin;

create table if not exists "MakeUp"
(
    "Id"            bigint generated always as identity primary key,
    "SortOrder"     integer not null default 0,
    "FilesInfo"     json    not null,
    "IsEnabled"     bool    not null default false
);

commit;