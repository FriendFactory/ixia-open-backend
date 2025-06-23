begin;

alter table "AiGeneratedVideoClip"
    add "UserSoundId" bigint null references "UserSound"("Id");

commit;