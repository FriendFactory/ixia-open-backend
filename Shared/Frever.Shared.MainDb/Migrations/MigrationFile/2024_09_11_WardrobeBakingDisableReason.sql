begin;

create table if not exists "WardrobeBakingDisableReason"
(
    "Id"            bigint generated always as identity primary key,
    "WardrobeId"    bigint references "Wardrobe"                            not null,
    "Reason"        text                                                    not null,
    "CreatedTime"   timestamp with time zone default CURRENT_TIMESTAMP      not null
);

create index if not exists "idx_WardrobeBakingDisableReason_Wardrobe"
    on "WardrobeBakingDisableReason" ("WardrobeId");

commit;