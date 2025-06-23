begin;

alter table "CharacterBakedViewHistory" alter column "ErrorCode" drop not null;

commit;