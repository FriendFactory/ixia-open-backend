begin;

alter table "BodyAnimation"
    add column "EditingAnimationForRaceIds"  bigint[],
    add column "BackgroundAnimationForRaceIds" bigint[];

commit;