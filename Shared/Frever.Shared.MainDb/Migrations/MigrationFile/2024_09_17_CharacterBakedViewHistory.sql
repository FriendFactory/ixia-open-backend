begin;

create table if not exists "CharacterBakedViewHistory"
(
    "Id"                    bigint generated always as identity primary key,
    "CharacterId"           bigint references "Character"                       not null,
    "CharacterBakedViewId"  bigint references "CharacterBakedView",
    "CreatedTime"           timestamp with time zone default CURRENT_TIMESTAMP  not null,
    "IsSuccessful"          boolean                                             not null,
    "ErrorCode"             text                                                not null,
    "CharacterModifiedTime" timestamp with time zone default CURRENT_TIMESTAMP  not null
);

create index if not exists "idx_CharacterBakedViewHistory_CharacterId"
    on "CharacterBakedViewHistory" ("CharacterId");

create index if not exists "idx_CharacterBakedViewHistory_CreatedTime"
    on "CharacterBakedViewHistory" ("CreatedTime");

commit;