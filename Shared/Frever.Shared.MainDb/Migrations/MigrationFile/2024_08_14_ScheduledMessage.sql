begin;

create table if not exists "ScheduledMessage"
(
    "Id"                        bigint generated always as identity,
    "SenderGroupId"             bigint                                               not null,
    "Status"                    text                                                 not null,
    "Text"                      text,
    "VideoId"                   bigint,
    "FilesInfo"                 json,
    "CreatedTime"               timestamp with time zone default CURRENT_TIMESTAMP   not null,
    "ModifiedTime"              timestamp with time zone default CURRENT_TIMESTAMP   not null,
    "ScheduledForTime"          timestamp with time zone default CURRENT_TIMESTAMP   not null,
    "GroupIds"                  bigint[],
    "CountryIds"                bigint[],
    "RegistrationAfterDate"     timestamp with time zone default CURRENT_TIMESTAMP,
    "RegistrationBeforeDate"    timestamp with time zone default CURRENT_TIMESTAMP,
    "LastLoginAfterDate"        timestamp with time zone default CURRENT_TIMESTAMP,
    "LastLoginBeforeDate"       timestamp with time zone default CURRENT_TIMESTAMP
);

alter table "ChatMessage" add column if not exists "ScheduledMessageId" bigint;

commit;