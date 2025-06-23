begin;

alter table "Character" add column "Version" uuid default uuid_generate_v4() not null;
alter table "CharacterBakedView" add column "CharacterVersion" uuid default '00000000-0000-0000-0000-000000000000';

update "CharacterBakedView" cbv
set "CharacterVersion" = (
    select "Version"
    from "Character" c
    where cbv."CharacterId" = c."Id"
)
where "IsValid" = true;

alter table "CharacterBakedView" alter column "CharacterVersion" drop default;

commit;