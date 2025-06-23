begin;


create table "AiGeneratedImage"
(
    "Id"                 bigint not null generated always as identity primary key,
    "GroupId"            bigint not null references "Group" ("Id"),
    "NumOfCharacters"    int    not null,
    "Seed"               int    not null,
    "Prompt"             text   not null,
    "ShortPromptSummary" text   null,
    "AiMakeupId"         bigint null references "AiMakeUp" ("Id"),
    "Files"              json   null
);

create table "AiGeneratedImagePerson"
(
    "Id"                             bigint not null generated always as identity primary key,
    "Ordinal"                        int    not null,
    "AiGeneratedImageId"             bigint not null references "AiGeneratedImage" ("Id"),
    "ParticipantGroupId"             bigint not null references "Group" ("Id"),
    "ParticipantAiCharacterSelfieId" bigint not null references "AiCharacterImage" ("Id"),
    "GenderId"                       bigint null references "Gender" ("Id"),
    "Files"                          json   null
);

create table "AiGeneratedImageSource"
(
    "Id"                 bigint not null generated always as identity primary key,
    "AiGeneratedImageId" bigint not null references "AiGeneratedImage" ("Id"),
    "Type"               text   not null,
    "Files"              json   null
);

create table "AiGeneratedVideo"
(
    "Id"             bigint not null generated always as identity primary key,
    "GroupId" bigint not null references "Group" ("Id"),
    "Type"           text   not null, -- 'pan', 'zoom', 'image-to-video'
    "LengthSec"      int,
    "ExternalSongId" bigint null,
    "IsLipSync"      bool default (false),
    "Files"          json   null
);

create table "AiGeneratedVideoClip"
(
    "Id"                 bigint not null generated always as identity primary key,
    "AiGeneratedVideoId" bigint not null references "AiGeneratedVideo" ("Id"),
    "Type"               text   not null, -- 'pan', 'zoom', 'image-to-video'
    "AiGeneratedImageId" bigint null references "AiGeneratedImage" ("Id"),
    "Ordinal"            int,
    "Prompt"             text,
    "ShortPromptSummary" text   null,
    "Seed"               int    null,
    "LengthSec"          int,
    "Files"              json   null
);

create table "AiGeneratedContent"
(
    "Id"                 bigint      not null generated always as identity primary key,
    "GroupId"            bigint      not null references "Group" ("Id"),
    "Access"             text        not null, -- private, public
    "Type"               text        not null, -- 'image', 'video'
    "AiGeneratedImageId" bigint      null references "AiGeneratedImage" ("Id"),
    "AiGeneratedVideoId" bigint      null references "AiGeneratedVideo" ("Id"),
    "ExternalSongId"     bigint      null,
    "IsLipSync"          bool        null,
    "CreatedAt"          timestamptz not null,
    "DeletedAt"          timestamptz null,
    constraint "AiGeneratedContent_Check_ContentRef" check (
        ("AiGeneratedImageId" is null and "AiGeneratedVideoId" is not null)
            or ("AiGeneratedImageId" is not null and "AiGeneratedVideoId" is null)
        )
);

commit;