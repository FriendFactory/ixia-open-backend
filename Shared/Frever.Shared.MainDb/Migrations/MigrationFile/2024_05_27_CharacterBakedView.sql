begin;

create table if not exists public."CharacterBakedView"
(
    "Id"            bigint  generated always as identity primary key,
    "CharacterId"   bigint  not null    references public."Character",
    "OutfitId"      bigint              references public."Outfit",
    "ReadinessId"   bigint  not null    references public."Readiness",
    "HeelsHeight"   real,
    "FilesInfo"     json,
    "CreatedTime"   timestamp with time zone default CURRENT_TIMESTAMP   not null,
    "ModifiedTime"  timestamp with time zone default CURRENT_TIMESTAMP   not null,
    "IsValid"       bool    not null    default false
);

create index if not exists "idx_baked_view_character_id"
    on public."CharacterBakedView" ("CharacterId");

create index if not exists "idx_baked_view_valid"
    on public."CharacterBakedView" ("IsValid");

create index if not exists "idx_baked_view_valid_character_id"
    on public."CharacterBakedView" ("CharacterId") where "IsValid";

commit;