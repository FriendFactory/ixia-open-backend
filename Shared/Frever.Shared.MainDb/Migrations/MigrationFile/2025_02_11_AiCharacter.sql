begin;


create table "AiCharacter"
(
    "Id"           bigint      not null generated always as identity primary key,
    "GroupId"      bigint      not null references "Group" ("Id"),
    "GenderId"     bigint      not null references "Gender" ("Id"),
    "Age"          int         null,
    "Name"         text        null,
    "Ethnicity"    text        null,
    "HairStyle"    text        null,
    "HairColor"    text        null,
    "FashionStyle" text        null,
    "Interests"    text        null,
    "Description"  text        null,
    "DeletedAt"    timestamptz null
);

create table "AiCharacterImage"
(
    "Id"               bigint       not null generated always as identity primary key,
    "AiCharacterId"    bigint       not null references "AiCharacter" ("Id"),
    "DetectedGenderId" bigint       null references "Gender" ("Id"),
    "Type"             varchar(128) not null check ("Type" in ('selfie', 'ai-face')),
    "Status"           varchar(128) null,
    "DetectedAge"      int          null,
    "AiModelRequest"   text         null,
    "AiModelResponse"  text         null,
    "Files"            json         null,
    "DeletedAt"        timestamptz  null
);

alter table "Group"
    add column "Files" json null;

commit;