
with g as (
    insert into "Group" ("NickName",
                         "TaxationCountryId",
                         "DefaultLanguageId",
                         "BirthDate",
                         "IsStarCreator",
                         "Gender"
        )
        values (:nickName,
                (select "Id" from "Country" where "ISOName" = :countryIso3),
                (select "Id" from "Language" where "IsoCode" = :languageIso3),
                :birthDate,
                :isStarCreator,
                1)
        returning *),
     u as (
         insert
             into "User" ("IdentityServerId", "Email", "DataCollection", "MainGroupId", "AnalyticsEnabled",
                          "PhoneNumber",
                          "CreatorPermissionLevel", "MainCharacterId")
                 select uuid_generate_v4(),
                        :email,
                        true,
                        g."Id",
                        true,
                        :phone,
                        array []::bigint[],
                        :mainCharacterId
                 from g
                 returning *),
     activity as (
         insert into "UserActivity" ("GroupId", "OccurredAt", "ActionType")
             select g."Id", :lastLogin, 'Login'
             from g
             where :lastLogin is not null)
select *
from u;