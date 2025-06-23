begin;

delete from "CharacterBakedView" c
where c."Id" in (
    select d."Id"
    from (
             select cbv."Id", ROW_NUMBER() over (partition by cbv."CharacterId", coalesce(cbv."OutfitId", 0) order by cbv."Id") as rn
             from "CharacterBakedView" cbv
         ) d where d.rn > 1
);

create unique index if not exists idx_unique_baked_view_character_outfit_null
    on "CharacterBakedView" ("CharacterId")
    where "OutfitId" is null;

create unique index if not exists idx_unique_baked_view_character_outfit
    on "CharacterBakedView" ("CharacterId", "OutfitId")
    where "OutfitId" is not null;

commit;