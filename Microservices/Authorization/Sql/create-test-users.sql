--------------------------------
--- Run on Auth Server DB    ---
--------------------------------
begin;

create extension if not exists "uuid-ossp";

delete
from "AspNetUsers"
where "PhoneNumber" like '+46755555%';


with ordinal as
             (select row_number() over () ord from "AspNetUsers"),
     user_num as
             (select ord from ordinal order by ord offset 30 limit 69),
     user_info as (
         select ord,
                concat('tester', ord::text, '@friendsfactory.xxx') as                                  username,
                'AQAAAAEAACcQAAAAEMXjUtylTHEqBMkicyIRPvUj9mxLJ6BSVjQlfU2fcLRJ57t+TeP7Q/4dkjoXNP+bSg==' password_hash,
                'EQYUN7U66JT75QCSSFQVZBOO7FQECD4N'                                                     security_stamp,
                '3a471362-ee48-47f2-b447-b4de494bc8b9'                                                 concurrency_stamp,
                concat('+46755555', ord::text)                                                         phone_number

         from user_num)
insert
into "AspNetUsers"
("Id", "UserName", "NormalizedUserName", "EmailConfirmed", "PasswordHash", "SecurityStamp",
 "ConcurrencyStamp", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
select concat('9c2fa5f8-4c62-4b6a-ae15-b9bd77b1e9', ord::text)::uuid,
       username,
       upper(username),
       false,
       password_hash,
       security_stamp,
       concurrency_stamp,
       phone_number,
       true,
       false,
       false,
       0
from user_info;

select *
from "AspNetUsers"
where "UserName" like '%.xxx';

commit;


--------------------------------
--- Run on Main Server DB    ---
--------------------------------
begin;

--set transaction isolation level serializable;

create extension if not exists "uuid-ossp";

delete
from "Group"
where exists(
              select 1
              from "UserAndGroup" ug
                       join
                   "User" u on ug."UserId" = u."Id"
              where ug."GroupId" = "Group"."Id"
                and u."PhoneNumber" like '+46755555%'
          );

delete
from "UserAndGroup"
where exists(
              select 1
              from "User" u
              where u."Id" = "UserAndGroup"."UserId"
                and u."PhoneNumber" like '+46755555%'
          );


delete
from "User"
where "PhoneNumber" like '+46755555%';

with ordinal as
             (select row_number() over () ord from "User"),
     user_num as
             (select ord from ordinal order by ord offset 30 limit 69),
     user_info as (
         select ord,
                concat('tester', ord::text, '@friendsfactory.xxx')            username,
                concat('tester-db-gen', ord::text)                            nickname,
                concat('+46755555', ord::text)                                phone_number,
                concat('9c2fa5f8-4c62-4b6a-ae15-b9bd77b1e9', ord::text)::uuid identity_server_id
         from user_num)
insert
into "Group" ("Name", "NickName", "IsPrimary", "ManagerId", "Private", "NumberOfLevels", "FanLevel",
              "CreatorLevel",
              "VerifiedGroup", "PremiumLevel", "BirthDate", "Gender", "IsBlocked")
select nickname,
       nickname,
       true,
       1,
       true,
       0,
       0,
       0,
       false,
       0,
       '2015-01-01'::date,
       1,
       false
from user_info;

with ordinal as
             (select row_number() over () ord from "User"),
     user_num as
             (select ord from ordinal order by ord offset 30 limit 69),
     user_info as (
         select ord,
                concat('tester', ord::text, '@friendsfactory.xxx')            username,
                concat('tester-db-gen', ord::text)                            nickname,
                concat('+46755555', ord::text)                                phone_number,
                concat('9c2fa5f8-4c62-4b6a-ae15-b9bd77b1e9', ord::text)::uuid identity_server_id
         from user_num)


insert
into "User" ("IdentityServerId", "Artist", "Qauser", "Partner", "Moderator", "DataCollection",
             "MainGroupId",
             "IsFeatured", "AnalyticsEnabled", "PhoneNumber",
             "CreatorPermissionLevel")
select identity_server_id,
       false,
       false,
       false,
       false,
       true,
       (select "Id" from "Group" where "NickName" = nickname),
       false,
       true,
       phone_number,
       '{}'
from user_info;

with ordinal as
             (select row_number() over () ord from "User"),
     user_num as
             (select ord from ordinal order by ord offset 30 limit 69),
     user_info as (
         select ord,
                concat('tester', ord::text, '@friendsfactory.xxx')            username,
                concat('tester-db-gen', ord::text)                            nickname,
                concat('+46755555', ord::text)                                phone_number,
                concat('9c2fa5f8-4c62-4b6a-ae15-b9bd77b1e9', ord::text)::uuid identity_server_id
         from user_num)
insert
into "UserAndGroup" ("UserId", "GroupId")
select (select "Id" from "User" where "PhoneNumber" = phone_number) userId,
       (select "Id" from "Group" where "NickName" = nickname)       groupId
from user_info;

select *
from "User"
where "PhoneNumber" like '+46755555%';

select *
from "Group"
where "NickName" like 'tester-db-gen%';

select *
from "UserAndGroup"
where "UserId" in (
    select "Id"
    from "User"
    where "PhoneNumber" like '+46755555%'
)
  and "GroupId" in (select "GroupId"
                    from "Group"
                    where "NickName" like 'tester-db-gen%');


commit;