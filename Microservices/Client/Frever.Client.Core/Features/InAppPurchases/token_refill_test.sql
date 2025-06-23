begin;

insert into "InAppProduct"("Title", "AppStoreProductRef", "PlayMarketProductRef",
                           "IsSeasonPass", "InAppProductPriceTierId", "SortOrder", "FilesInfo",
                           "IsSubscription", "DailyHardCurrency",
                           "MonthlyHardCurrency", "Files")
values ('test', 'test_appstore', 'test_playmarket',
        false, 1, 0, '[]', true, 30, 1500, '[]');

with product as (select *
                 from "InAppProduct"
                 where "IsSubscription"
                   and "IsActive"
                   and not "IsFreeProduct"
                 limit 1)
insert
into "InAppUserSubscription" ("GroupId", "Status", "StartedAt", "CompletedAt", "RefInAppProductId",
                              "DailyHardCurrency", "MonthlyHardCurrency")
values (433, 'Active', current_timestamp - interval '20 day', current_timestamp + interval '10 day',
        (select "Id" from product limit 1),
        40,
        0);

insert into "AssetStoreTransaction"("CreatedTime", "GroupId", "TransactionType", "HardCurrencyAmount",
                                    "SoftCurrencyAmount")
values ('2025-01-01 10:00:00', 433, 'InAppPurchase', 1000, 0),
       -- ^^ IA: D = 0     S = 0     P = 1000       T = 1000
       ('2025-01-02 11:00:00', 433, 'DailyTokenRefill', 30, 0),
       ('2025-01-02 11:15:00', 433, 'AiWorkflowRun', -100, 0),
       -- ^^ D0: D = 0     S = 0     P = 930       T = 930
       ('2025-01-03 11:00:00', 433, 'DailyTokenRefill', 30, 0),
       ('2025-01-03 11:10:00', 433, 'AiWorkflowRun', -10, 0),
       -- ^^ D1: D = 20     S = 0     P = 930       T = 950
       ('2025-01-04 11:00:00', 433, 'DailyTokenBurnout', -20, 0),
       ('2025-01-04 11:00:01', 433, 'DailyTokenRefill', 30, 0),
       -- ^^ D2: D = 30     S = 0     P = 930       T = 960
       ('2025-01-05 11:00:00', 433, 'AiWorkflowRun', -100, 0),
       -- ^^ D3: D = 0     S = 0     P = 860       T = 860
       ('2025-01-06 11:00:00', 433, 'DailyTokenRefill', 30, 0),
       ('2025-01-06 11:10:00', 433, 'AiWorkflowRun', -15, 0),
       -- ^^ D4: D = 15     S = 0     P = 860       T = 875
       ('2025-01-07 11:00:00', 433, 'DailyTokenBurnout', -15, 0),
       ('2025-01-07 11:00:01', 433, 'DailyTokenRefill', 30, 0),
       ('2025-01-07 11:30:00', 433, 'MonthlyTokenRefill', 1500, 0),
       -- ^^ D5: D = 30     S = 1500     P = 860       T = 2390
       ('2025-01-08 11:00:00', 433, 'AiWorkflowRun', -10, 0),
       -- ^^ D6: D = 20     S = 1500     P = 860       T = 2380
       ('2025-01-09 11:00:00', 433, 'DailyTokenBurnout', -20, 0),
       ('2025-01-09 11:00:01', 433, 'DailyTokenRefill', 30, 0),
       -- ^^ D7: D = 30     S = 1500     P = 860       T = 2390
       ('2025-01-10 11:00:00', 433, 'AiWorkflowRun', -1700, 0),
       -- ^^ D8: D = 0     S = 0     P = 690       T = 690
       ('2025-01-11 11:00:00', 433, 'DailyTokenRefill', 30, 0),
       -- ^^ D9: D = 30     S = 0     P = 690       T = 720
       ('2025-01-12 11:00:00', 433, 'AiWorkflowRun', -20, 0),
       -- ^^ D10: D = 10     S = 0     P = 690       T = 700
       ('2025-01-13 11:00:00', 433, 'DailyTokenBurnout', -10, 0),
       ('2025-01-13 11:00:01', 433, 'DailyTokenRefill', 30, 0),
       ('2025-01-13 11:00:01', 433, 'InAppPurchase', 500, 0),
       -- ^^ D11: D = 30     S = 0     P = 1190         T = 1120
       ('2025-01-14 11:00:00', 433, 'MonthlyTokenRefill', 1500, 0),
       ('2025-01-14 11:10:00', 433, 'AiWorkflowRun', -100, 0),
       -- ^^ D12: D = 0     S = 1430     P = 1190       T = 2620
       ('2025-01-15 11:00:00', 433, 'DailyTokenRefill', 30, 0),
       -- ^^ D13: D = 30     S = 1430     P = 1190      T = 2620
       ('2025-01-16 11:00:00', 433, 'MonthlyTokenBurnout', -1430, 0),
       -- CHECKPOINT: Here Balance = Permanent + Daily
       ('2025-01-16 11:00:01', 433, 'MonthlyTokenRefill', 1500, 0),
       -- ^^ D14: D = 30     S = 1500     P = 1190      T = 2720
       ('2025-01-17 11:00:00', 433, 'AiWorkflowRun', -100, 0),
       -- ^^ D15: D = 0     S = 1430     P = 1190       T = 2620
       ('2025-01-18 11:00:00', 433, 'DailyTokenRefill', 30, 0),
       ('2025-01-18 11:00:00', 433, 'AiWorkflowRun', -10, 0),
--------- ^^ D16: D = 20     S = 1430     P = 1190      T = 2640
       (current_timestamp - interval '1 day', 433, 'DailyTokenBurnout', -20, 0),
       (current_timestamp - interval '1 day', 433, 'DailyTokenRefill', 30, 0),
       (current_timestamp, 433, 'AiWorkflowRun', -11, 0)
;

with settings as (select 30                 as default_daily_tokens,
                         1111               as system_group_id,
                         uuid_generate_v4() as transaction_group)
   , transaction_types_to_generate as (select 'SystemIncome'::"AssetStoreTransactionType" as type
                                       union all
                                       select 'SystemExpense'::"AssetStoreTransactionType" as type
                                       union all
                                       select 'DailyTokenRefill' as type
                                       union all
                                       select 'DailyTokenBurnout' as type)
   , default_subscription as (select *
                              from "InAppProduct"
                              where not "IsActive"
                                and "IsSubscription"
                                and "IsFreeProduct"
                              limit 1)
   , active_subscriptions as (select *,
                                     row_number() over (partition by "GroupId" order by "Id" desc) as rn
                              from "InAppUserSubscription"
                              where "StartedAt"::date < current_timestamp::date
                                and "CompletedAt"::date >= current_timestamp::date)
   , latest_active_subscriptions as (select *
                                     from active_subscriptions
                                     where rn = 1)
   , users_with_daily_refill_amount as (select g."Id"                           as group_id,
                                               a."Id"                           as active_subscription_id,
                                               coalesce(a."DailyHardCurrency", d."DailyHardCurrency",
                                                        f.default_daily_tokens) as daily_tokens

                                        from "Group" as g
                                                 left join latest_active_subscriptions as a on a."GroupId" = g."Id"
                                                 left join
                                             default_subscription as d on true
                                                 cross join settings as f)
   , daily_refill_ranked as (select *
                                  , row_number() over (partition by "GroupId" order by "Id" desc) as rn
                             from "AssetStoreTransaction" as t
                             where t."TransactionType"::text = 'DailyTokenRefill')
   , last_daily_refill as (select *,
                                  "CreatedTime" as last_refill_date
                           from daily_refill_ranked
                           where rn = 1
                             and "GroupId" not in (select system_group_id from settings))
   , post_refill_transactions as (select tr.*,
                                         last_refill.last_refill_date
                                  from last_daily_refill as last_refill
                                           inner join
                                       "AssetStoreTransaction" tr on last_refill."GroupId" = tr."GroupId"
                                           and tr."Id" >= last_refill."Id")
   , post_refill_expenses_daily as (select *
                                    from post_refill_transactions
                                    where "TransactionType" = 'DailyTokenRefill'
                                       or "HardCurrencyAmount" < 0)
   , post_refill_balance as (select "GroupId",
                                    sum("HardCurrencyAmount") as balance,
                                    last_refill_date
                             from post_refill_expenses_daily
                             group by "GroupId", last_refill_date)
   , final as (select au.group_id            as "GroupId",
                      coalesce(r.balance, 0) as balance,
                      au.daily_tokens        as daily_refill_amount,
                      au.active_subscription_id
               from users_with_daily_refill_amount au
                        left join
                    post_refill_balance as r on au.group_id = r."GroupId"
               where (r.last_refill_date is null
                   or r.last_refill_date::date < current_timestamp::date)
                 and (r.balance is null or r.balance < au.daily_tokens))
   , generated_transactions as (select case
                                           when tt.type in ('SystemIncome', 'SystemExpense')
                                               then settings.system_group_id
                                           else t."GroupId" end   as "GroupId",
                                       settings.transaction_group as "TransactionGroup",
                                       tt.type                    as "TransactionType",
                                       0                          as "SoftCurrencyAmount",
                                       case
                                           when tt.type = 'SystemIncome' then greatest(0, t.balance)
                                           when tt.type = 'DailyTokenBurnout' then -greatest(0, t.balance)
                                           when tt.type = 'DailyTokenRefill' then t.daily_refill_amount
                                           when tt.type = 'SystemExpense' then -t.daily_refill_amount
                                           end                    as "HardCurrencyAmount"
                                from final t
                                         cross join
                                     transaction_types_to_generate tt
                                         cross join settings)
   , non_zero_transactions as
    (select *
     from generated_transactions
     where "HardCurrencyAmount" <> 0)
insert
into "AssetStoreTransaction"("GroupId", "TransactionType", "TransactionGroup", "SoftCurrencyAmount",
                             "HardCurrencyAmount")
select "GroupId", "TransactionType", "TransactionGroup", "SoftCurrencyAmount", "HardCurrencyAmount"
from non_zero_transactions
--    select * from last_daily_refill
;

select *
from "AssetStoreTransaction"
where "GroupId" = 433
order by "Id" desc;

rollback;