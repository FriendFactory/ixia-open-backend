begin;

create table if not exists "BodyAnimationAndVfx"
(
    "BodyAnimationId"   bigint not null references "BodyAnimation",
    "VfxId"             bigint not null references "Vfx",
    "StartTime"         int,
    "EndTime"           int,
    primary key ("BodyAnimationId", "VfxId")
);

alter table "VfxCategory" add column "AllowAnimation" bool default false not null;

commit;