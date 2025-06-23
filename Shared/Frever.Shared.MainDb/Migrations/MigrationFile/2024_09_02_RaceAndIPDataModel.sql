begin;

create table "IntellectualProperty"
(
    "Id"          bigint not null generated always as identity primary key,
    "Name"        text   not null,
    "ReadinessId" bigint not null references "Readiness"
);

insert into "IntellectualProperty" ("Id", "Name", "ReadinessId") overriding system value
values (1, 'Frever', 2),
       (2, 'EA Games', 2);
alter sequence "IntellectualProperty_Id_seq" restart with 3;

create table "Race"
(
    "Id"                     bigint not null generated always as identity primary key,
    "IntellectualPropertyId" bigint not null references "IntellectualProperty",
    "Name"                   text   not null,
    "ReadinessId"            bigint not null references "Readiness",
    "Prefab"                 text   not null
);

insert into "Race" ("Id", "IntellectualPropertyId", "Name", "ReadinessId", "Prefab") overriding system value
values (1, 1, 'Frever', 2, 'frever_default'),
       (2, 2, 'Sims', 2, 'sims_prefab');
alter sequence "Race_Id_seq" restart with 3;

alter table "Gender"
    add "RaceId"     bigint not null references "Race" default (1),
    add "Identifier" text null check ("Identifier" is null or "Identifier" in ('male', 'female', 'non-binary')),
    add "UmaRaceKey" text,
    add "UpperUnderwearOverlay" text,
    add "LowerUnderwearOverlay" text;

update "Gender" set "Identifier" = lower("Name");

update "Gender"
set "UmaRaceKey" = 'Male Base',
    "LowerUnderwearOverlay" = 'underwear_M_Shorts_v1_Overlay'
where "Id" = 1;

update "Gender"
set "UmaRaceKey" = 'Female Base',
    "UpperUnderwearOverlay" = 'underwear_F_Bra_v1_Overlay',
    "LowerUnderwearOverlay" = 'underwear_F_Shorts_v1_Overlay'
where "Id" = 2;

update "Gender"
set "UmaRaceKey" = 'Non Binary Base',
    "UpperUnderwearOverlay" = 'underwear_F_Bra_v1_Overlay',
    "LowerUnderwearOverlay" = 'underwear_F_Shorts_v1_Overlay'
where "Id" = 3;

insert into "Gender" ("Id", "Name", "RaceId", "Identifier", "UmaRaceKey", "UpperUnderwearOverlay", "LowerUnderwearOverlay") overriding system value
    values (4, 'Sims Female', 2, 'female', 'Sims Female Base', 'sims_Female_Underwear_Bra', 'sims_Female_Underwear_Shorts_Overlay'),
           (5, 'Sims Male', 2, 'male', 'Sims Male Base', null, 'sims_Male_Underwear_Overlay'),
           (6, 'Sims Non-binary', 2, 'non-binary', 'Non Binary Sims Base','sims_Female_Underwear_Bra', 'sims_Female_Underwear_Shorts_Overlay');
alter sequence "Gender_Id_seq" restart with 7;

alter table "Gender" alter column "Identifier" set not null;
alter table "Gender" drop constraint if exists "Gender_Name_key";
alter table "Gender" alter column "UmaRaceKey" set not null;
alter table "Gender" alter column "LowerUnderwearOverlay" set not null;

create table "Universe"
(
    "Id"                bigint    not null generated always as identity primary key,
    "Name"              text      not null,
    "ReadinessId"       bigint    not null references "Readiness",
    "FilesInfo"         json      not null,
    "IsNew"             boolean   not null default false,
    "SortOrder"         integer   not null default 0,
    "AllowStartGift"    boolean   not null default false,
    "AllAssetsFree"     boolean   not null default false
);

insert into "Universe"("Id", "Name", "ReadinessId", "FilesInfo", "IsNew", "SortOrder", "AllowStartGift", "AllAssetsFree")
    overriding system value
values (1, 'Frever', 2, '[]', false, 2, true, false),
       (2, 'Sims', 2, '[]', true, 1, false, true);
alter sequence "Universe_Id_seq" restart with 3;

create table "UniverseAndRace"
(
    "UniverseId"  bigint not null references "Universe",
    "RaceId"      bigint not null references "Race",
    "ReadinessId" bigint not null references "Readiness",
    "Settings"    json   not null,
    primary key ("UniverseId", "RaceId")
);

insert into "UniverseAndRace"("UniverseId", "RaceId", "ReadinessId", "Settings")
values (1, 1, 2, '{"CanUseCharacters": true,"CanRemixVideos": true,"SupportsSelfieToAvatar": true,"CanCreateCharacters": true}'),
       (2, 2, 2, '{"CanUseCharacters": false,"CanRemixVideos": false,"SupportsSelfieToAvatar": false,"CanCreateCharacters": false}');

alter table "BodyAnimation" add "CompatibleRaceIds" bigint[] null;
alter table "CameraFilter" add "CompatibleRaceIds" bigint[] null;
alter table "Prop" add "CompatibleRaceIds" bigint[] null;
alter table "SetLocation" add "CompatibleRaceIds" bigint[] null;
alter table "SetLocationAndCharacterSpawnPosition" add "CompatibleRaceIds" bigint[] null;
alter table "Vfx" add "CompatibleRaceIds" bigint[] null;
alter table "VoiceFilter" add "CompatibleRaceIds" bigint[] null;

alter table "Wardrobe"
    add "CompatibleGenderIds" bigint[] null,
    add "PurchaseGroup"       int      not null default (0);

update "Wardrobe"
set "CompatibleGenderIds" =
    case
        when "GenderId" = 2 then ARRAY[2, 3]::bigint[]
        when "GenderId" = 3 then null
        else ARRAY ["GenderId"]
    end;

alter table "Video"
    add "RaceIds"    bigint[] not null default (ARRAY [1]),
    add "UniverseId" bigint   not null default (1) references "Universe";

alter table "ThemeCollection"
    add "UniverseId" bigint not null default (1) references "Universe";

alter table "UmaBundle" add "GenderIds" bigint[];

update "UmaBundle" set "GenderIds" = '{2,3}' where "AssetBundleName" = 'global_female_base';
update "UmaBundle" set "GenderIds" = '{1}' where "AssetBundleName" = 'global_male_base';
update "UmaBundle" set "GenderIds" = '{4,5,6}' where "AssetBundleName" = 'global_sims_shared';
update "UmaBundle" set "GenderIds" = '{4,6}' where "AssetBundleName" = 'global_sims_female_base';
update "UmaBundle" set "GenderIds" = '{5}' where "AssetBundleName" = 'global_sims_male_base';
update "UmaBundle" set "GenderIds" = '{1,2,3,4,5,6}' where "AssetBundleName" = 'global_shared';

create table if not exists "GroupMainCharacter"
(
    "GroupId"       bigint                                             not null
    references public."Group",
    "UniverseId"    bigint                                             not null
    references public."Universe",
    "CharacterId"   bigint                                             not null
    references public."Character",
    "Time"          timestamp with time zone default CURRENT_TIMESTAMP not null,
    primary key ("GroupId", "UniverseId")
);

create index if not exists "idx_group_main_character_" on "GroupMainCharacter" ("GroupId");

insert into "GroupMainCharacter" ("GroupId", "UniverseId", "CharacterId", "Time")
select u."MainGroupId", 1, u."MainCharacterId", now()
from "User" u
where u."MainCharacterId" is not null;

commit;