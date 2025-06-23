begin;

alter table "Wardrobe" add column if not exists "AvailableForBaking" bool not null default true;

update "Wardrobe" w
set "AvailableForBaking" = false
where w."Name" ilike '%wing%';

create index if not exists "idx_wardrobe_available_for_baking"
    on "Wardrobe" ("Id", "AvailableForBaking");

create index if not exists "idx_wardrobe_not_available_for_baking"
    on "Wardrobe" ("Id") where ("AvailableForBaking" = false);

create index if not exists idx_baked_view_character_id_and_version
    on "CharacterBakedView" ("CharacterId", "CharacterVersion");

commit;