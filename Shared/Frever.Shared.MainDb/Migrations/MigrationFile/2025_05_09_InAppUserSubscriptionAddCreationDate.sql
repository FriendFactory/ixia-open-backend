begin;

alter table "InAppUserSubscription"
    add "CreatedAt" timestamptz not null default (current_timestamp);

update "InAppUserSubscription"
set "CreatedAt" = "StartedAt";

commit;