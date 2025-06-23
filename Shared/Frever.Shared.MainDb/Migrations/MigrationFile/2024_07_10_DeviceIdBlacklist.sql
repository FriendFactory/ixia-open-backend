begin;

create table "DeviceBlacklist"
(
    "DeviceId"         text        not null primary key,
    "BlockedAt"        timestamptz not null default (current_timestamp),
    "BlockedByGroupId" bigint not null references "Group",
    "Reason"           text
);


commit;