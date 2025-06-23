begin;

create table if not exists "TransformationStyle"
(
    "Id"                    bigint generated always as identity
    primary key,
    "Name"                  text              not null,
    "Value"                 text              not null,
    "SortOrder"             integer default 0 not null,
    "FilesInfo"             json              not null,
    "DefaultPositivePrompt" text,
    "DefaultNegativePrompt" text
);

insert into "TransformationStyle" ("Name", "Value", "SortOrder", "FilesInfo")
values ('Default', 'default', 0, '[]'),
       ('Anime', 'anime', 1, '[]'),
       ('Cartoon', 'cartoon', 2, '[]'),
       ('Realism', 'realism', 3, '[]'),
       ('VFI', 'vfi', 4, '[]');

alter table "Video"
    add column "StyleId" bigint references "TransformationStyle" ("Id"),
    add column "TransformedFromVideoId" bigint references "Video" ("Id"),
    add column "TransformationCompleted" bool;

alter type "NotificationType" add value 'VideoStyleTransformed';

commit;