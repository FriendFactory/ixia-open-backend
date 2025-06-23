begin;

alter table "CharacterSpawnPositionFormation"
    add column if not exists "ApplyOnCharacterEditing" boolean default false not null;

create unique index if not exists "SpawnPositionFormation_CharacterCount_ApplyOnCharacterEditing"
    on "CharacterSpawnPositionFormation" ("CharacterCount")
    where "ApplyOnCharacterEditing" is true;

commit;