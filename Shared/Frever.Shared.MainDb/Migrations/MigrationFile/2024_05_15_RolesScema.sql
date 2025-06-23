begin;

create schema if not exists cms;

create table cms.databasechangeloglock
(
    id          integer not null primary key,
    locked      boolean not null,
    lockgranted timestamp,
    lockedby    varchar(255)
);

create table cms.databasechangelog
(
    id            varchar(255) not null,
    author        varchar(255) not null,
    filename      varchar(255) not null,
    dateexecuted  timestamp    not null,
    orderexecuted integer      not null,
    exectype      varchar(10)  not null,
    md5sum        varchar(35),
    description   varchar(255),
    comments      varchar(255),
    tag           varchar(255),
    liquibase     varchar(20),
    contexts      varchar(255),
    labels        varchar(255),
    deployment_id varchar(10)
);

insert into cms.databasechangelog values ('create-tables', 'xxd', 'db/migrations/000001_init_tables.sql', '2023-03-17 13:19:40.251020', 1, 'EXECUTED', '9:7e9bfb102ccc544dd4ba48e88d00a4f1', 'sql', '', NULL, '4.16.1', NULL, NULL, '9059180040');
insert into cms.databasechangelog values ('add-design-role', 'xxd', 'db/migrations/000002_add_design_role.sql', '2023-03-28 14:59:18.200455', 2, 'EXECUTED', '9:6857091092d65d0a76bdea7bf67bee9d', 'sql', '', NULL, '4.16.1', NULL, NULL, '0015558145');

create table cms.role
(
    id         serial                  primary key,
    name       varchar(128)            not null,
    created_at timestamp default now() not null
);

create unique index role_name_index
    on cms.role (name);

create table cms.role_access_scope
(
    role_id         integer                 not null
        references cms.role,
    access_scope    varchar(128)            not null,
    created_at      timestamp default now() not null,
    primary key (role_id, access_scope)
);

create table cms.user_role
(
    group_id      bigint            not null,
    role_id       integer           not null
        references cms.role,
    created_at timestamp default now() not null,
    primary key (group_id, role_id)
);

alter table public."Group" add column "IsOfficial" boolean not null default false;

create table if not exists public."ChatOfficialAccess"
(
    "GroupId"                   bigint                      not null,
    "FreverOfficialGroupId"     bigint                      not null,
    "CreatedTime"               timestamp default now()     not null,

    primary key ("GroupId", "FreverOfficialGroupId")
);

alter table public."Group"
drop column "IsPrimary",
    drop column "ManagerId",
    drop column "CurrencyId",
    drop column "Private",
    drop column "NumberOfLevels",
    drop column "FanLevel",
    drop column "CreatorLevel",
    drop column "VerifiedGroup",
    drop column "PremiumLevel",
    drop column "StatusId",
    drop column "Limitations",
    drop column "SavedOnlyBdayYear",
    drop column "ParentEmail";

alter table public."User"
drop column "ConsentTime",
    drop column "Artist",
    drop column "Qauser",
    drop column "Partner",
    drop column "Moderator",
    drop column "IsEmployee",
    drop column "IsCodeRequired";

alter table public."Character"
drop column "DefaultOutfitId";

alter table public."Level"
drop column "LevelTemplateId",
    drop column "LanguageId",
    drop column "VerticalCategoryId";

alter table public."VoiceTrack"
drop column "LanguageId";

alter table public."FaceAnimation"
drop column "FaceAnimationCategoryId";

alter table public."SetLocationController"
drop column "WeatherId";

commit;