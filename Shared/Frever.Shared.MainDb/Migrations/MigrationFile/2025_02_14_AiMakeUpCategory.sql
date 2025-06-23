begin;

create table if not exists "AiMakeUpCategory"
(
    "Id"        bigint generated always as identity primary key,
    "SortOrder" integer default 0       not null,
    "Name"      text                    not null,
    "IsPreset"  bool    default false   not null,
    "Workflow"  text    unique          null unique
);

insert into "AiMakeUpCategory" ("SortOrder", "Name")
values (1, 'General');

alter table "MakeUp"
    add column "CategoryId" bigint references "AiMakeUpCategory" not null default 1;

alter table "MakeUp"
    rename to "AiMakeUp";

commit;