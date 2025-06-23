begin;

create table if not exists "AiMainTheme"
(
    "Id"        bigint generated always as identity primary key,
    "Name"      text                  not null,
    "IsEnabled" boolean default false not null,
    "SortOrder" integer default 0     not null
);

commit;