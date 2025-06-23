begin;

alter table "CharacterSpawnPosition" add column "Adjustments" text;

update "CharacterSpawnPosition"
set "Adjustments" = '[{"GenderIds":[4,5,6],"Scale":0.75,"AdjustY":-0.043}]'
where "MovementTypeId" = 2 and "Adjustments" is null;

commit;