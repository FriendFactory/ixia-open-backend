begin;

alter table "InAppProduct"
    add "IsSubscription"      bool default (false),
    add "DailyHardCurrency"   int  default (60),
    add "MonthlyHardCurrency" int  default (0);

create table "InAppUserSubscription"
(
    "Id"                  bigint    not null generated always as identity primary key,
    "GroupId"             bigint    not null references "Group" ("Id"),
    "Status"              text      not null, -- active, cancelled, refund
    "StartedAt"           timestamp not null,
    "CompletedAt"         timestamp null,
    "RefInAppProductId"      bigint    not null references "InAppProduct" ("Id"),
    "DailyHardCurrency"   int       not null,
    "MonthlyHardCurrency" int       not null
);

alter table "AssetStoreTransaction"
    add "InAppUserSubscriptionId" bigint null references "InAppUserSubscription"("Id");

alter type "AssetStoreTransactionType" add value 'DailyTokenRefill';
alter type "AssetStoreTransactionType" add value 'MonthlyTokenRefill';
alter type "AssetStoreTransactionType" add value 'DailyTokenBurnout';
alter type "AssetStoreTransactionType" add value 'MonthlyTokenBurnout';

commit;